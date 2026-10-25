#!/usr/bin/env bash
# Azure steps for the Northline desk. The GitHub workflow calls this.
# A person runs it only after the one-time setup in docs/azure-setup.md.
set -euo pipefail

RG="${AZURE_RESOURCE_GROUP:?Set AZURE_RESOURCE_GROUP}"
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

require_uksouth() {
  local loc
  loc="$(az group show -n "$RG" --query location -o tsv)"
  if [[ "$loc" != "uksouth" ]]; then
    echo "Resource group $RG is in $loc. Create it in uksouth." >&2
    exit 1
  fi
}

load_outputs() {
  local json
  json="$(az deployment group show -g "$RG" -n northlinedesk --query properties.outputs -o json)"
  DESK="$(jq -r .deskName.value <<<"$json")"
  API="$(jq -r .apiName.value <<<"$json")"
  SQL_SERVER="$(jq -r .sqlServerName.value <<<"$json")"
  SQL_LOGIN="$(jq -r '.sqlAdminLogin.value // empty' <<<"$json")"
    if [[ -z "$SQL_LOGIN" || "$SQL_LOGIN" == "null" ]]; then
    SQL_LOGIN=northlineadmin
    fi
  VAULT="$(jq -r .vaultName.value <<<"$json")"
  DESK_HOST="$(jq -r .deskHost.value <<<"$json")"
  API_HOST="$(jq -r .apiHost.value <<<"$json")"
  STAGING_HOST="$(jq -r .stagingHost.value <<<"$json")"
}

open_firewall() {
  RUNNER_IP="$(curl -fsS https://api.ipify.org)"
  az sql server firewall-rule create \
    -g "$RG" -s "$SQL_SERVER" -n "gha-${GITHUB_RUN_ID:-local}" \
    --start-ip-address "$RUNNER_IP" --end-ip-address "$RUNNER_IP" \
    --output none
}

close_firewall() {
  az sql server firewall-rule delete \
    -g "$RG" -s "$SQL_SERVER" -n "gha-${GITHUB_RUN_ID:-local}" \
    --output none || true
}

sql_connection() {
  printf 'Server=tcp:%s.database.windows.net,1433;Initial Catalog=NorthlineDesk;User ID=%s;Password=%s;Encrypt=True;TrustServerCertificate=False;' \
    "$SQL_SERVER" "$SQL_LOGIN" "$SQL_ADMIN_PASSWORD"
}

put_secrets() {
  local connection
  connection="$(sql_connection)"
  az keyvault secret set --vault-name "$VAULT" --name sql-connection --value "$connection" --output none
  az keyvault secret set --vault-name "$VAULT" --name shadow-key --value "$SHADOW_KEY" --output none
}

deploy_infra() {
  require_uksouth
  : "${SQL_ADMIN_PASSWORD:?Set SQL_ADMIN_PASSWORD}"
  : "${AZURE_OBJECT_ID:?Set AZURE_OBJECT_ID to the enterprise application object id}"
  local extra=()
  if [[ -n "${BUDGET_EMAIL:-}" ]]; then
    extra+=(--parameters "budgetEmail=${BUDGET_EMAIL}" "budgetStart=${BUDGET_START:-2026-10-01}")
  fi
  az deployment group create \
    -g "$RG" \
    -n northlinedesk \
    -f "$ROOT/infra/main.bicep" \
    -p "$ROOT/infra/main.bicepparam" \
    --parameters "sqlAdminPassword=${SQL_ADMIN_PASSWORD}" "deployerObjectId=${AZURE_OBJECT_ID}" \
    "${extra[@]}" \
    --output none
}

migrate() {
  local through="${1:-1}"
  load_outputs
  open_firewall
  trap close_firewall RETURN
  NORTHLINE_SQL="$(sql_connection)" dotnet run --project "$ROOT/tools/Northline.Migrator" -- --through "$through"
}

smoke_slots() {
  load_outputs
  local production="https://${DESK_HOST}/Match.aspx?invoice=INV-1001"
  local staging="https://${STAGING_HOST}/Match.aspx?invoice=INV-1001"
  wait_for "$production" 'data-decision-path="stored-procedure"'
  wait_for "$staging" 'data-decision-path="match-service"'
  local match
  match="$(curl -fsS -X POST "https://${API_HOST}/match" -H 'Content-Type: application/json' -d '{"invoiceNumber":"INV-1001"}')"
  echo "$match" | jq -e '.approved == 612 and .unmatched == 6' >/dev/null
  local shadow
  shadow="$(curl -fsS -X POST "https://${API_HOST}/shadow/INV-1001" -H "X-Shadow-Key: ${SHADOW_KEY}")"
  echo "$shadow"
  echo "$shadow" | jq -e '.promotionBlocked == true and .legacyApproved == 1224 and .serviceApproved == 612' >/dev/null
  echo "Production is the procedure. Staging is the service. Promotion is blocked."
}

promote() {
  : "${SHADOW_KEY:?}"
  load_outputs
  open_firewall
  trap close_firewall RETURN
  NORTHLINE_SQL="$(sql_connection)" dotnet run --project "$ROOT/tools/Northline.Migrator" -- --explain --through 1
  local shadow
  shadow="$(curl -fsS -X POST "https://${API_HOST}/shadow/INV-1001" -H "X-Shadow-Key: ${SHADOW_KEY}")"
  echo "$shadow"
  echo "$shadow" | jq -e '.promotionBlocked == false' >/dev/null
  az webapp deployment slot swap -g "$RG" -n "$DESK" --slot staging --target-slot production --output none
  NORTHLINE_SQL="$(sql_connection)" dotnet run --project "$ROOT/tools/Northline.Migrator" -- --through 2 --assert-retired
  wait_for "https://${DESK_HOST}/Match.aspx?invoice=INV-1001" 'data-decision-path="match-service"'
  wait_for "https://${DESK_HOST}/Match.aspx?invoice=INV-1001" '£612.00'
  local staging_body
  staging_body="$(curl -sS -o /tmp/staging.html -w '%{http_code}' "https://${STAGING_HOST}/Match.aspx?invoice=INV-1001" || true)"
  if ! grep -q 'stored procedure is gone' /tmp/staging.html; then
    echo "Staging should now fail on the retired procedure. HTTP ${staging_body}." >&2
    cat /tmp/staging.html >&2 || true
    exit 1
  fi
  echo "Production shows the match service. The procedure object id is null."
}

reset_demo() {
  : "${SHADOW_KEY:?}"
  load_outputs
  open_firewall
  trap close_firewall RETURN
  NORTHLINE_SQL="$(sql_connection)" dotnet run --project "$ROOT/tools/Northline.Migrator" -- --reset-to 1 --through 1
  az webapp config appsettings set -g "$RG" -n "$DESK" --settings MatchMode=Procedure --output none
  az webapp config appsettings set -g "$RG" -n "$DESK" --slot staging --settings MatchMode=Api --output none
  echo "Production is back on the procedure. Staging is back on the service. Shadow rows are cleared."
}

wait_for() {
  local url="$1"
  local needle="$2"
  local attempt body
  for attempt in $(seq 1 24); do
    body="$(curl -fsS "$url" || true)"
    if grep -q "$needle" <<<"$body"; then
      return 0
    fi
    sleep 10
  done
  echo "Timed out waiting for [$needle] at $url" >&2
  printf '%s\n' "$body" >&2
  return 1
}

case "${1:-}" in
  infra) deploy_infra ;;
  secrets) load_outputs; put_secrets ;;
  migrate) migrate "${2:-1}" ;;
  smoke) smoke_slots ;;
  promote) promote ;;
  reset) reset_demo ;;
  *)
    echo "Use: infra | secrets | migrate [n] | smoke | promote | reset" >&2
    exit 1
    ;;
esac
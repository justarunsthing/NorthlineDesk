<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="PurchaseOrder.aspx.cs" Inherits="NorthlineDesk.PurchaseOrderPage" %>
<!DOCTYPE html>
<html lang="en-GB">
<head>
  <meta charset="utf-8" />
  <title>Purchase order <%= Server.HtmlEncode(OrderNumber) %></title>
  <link rel="icon" href="data:," />
  <link rel="stylesheet" href="site.css" />
</head>
<body>
  <div class="wrap">
    <header>
      <p class="mark">Northline desk</p>
      <h1><%= Server.HtmlEncode(OrderNumber) %></h1>
    </header>
    <nav>
      <a href="Default.aspx">Desk</a>
      <a href="Match.aspx?invoice=INV-1001">Match INV-1001</a>
    </nav>
    <% if (HasError) { %>
      <p class="error"><%= Server.HtmlEncode(ErrorMessage) %></p>
    <% } else { %>
      <p><%= Server.HtmlEncode(Supplier) %>, <%= Server.HtmlEncode(Depot) %></p>
      <p>Approved by <%= Server.HtmlEncode(Approver) %> on <%= Server.HtmlEncode(ApprovedOn) %>.</p>
      <table>
        <thead>
          <tr>
            <th>Callout</th>
            <th class="num">Ordered</th>
            <th class="num">Received</th>
            <th class="num">Net</th>
          </tr>
        </thead>
        <tbody>
          <% foreach (var line in Lines) { %>
          <tr class="<%= line.QuantityReceived == 0 ? "missing" : "" %>">
            <td><%= Server.HtmlEncode(line.Description) %></td>
            <td class="num"><%= line.QuantityOrdered.ToString("0") %></td>
            <td class="num"><%= line.QuantityReceived.ToString("0") %></td>
            <td class="num"><%= line.UnitPrice.ToString("C", Pounds) %></td>
          </tr>
          <% } %>
        </tbody>
      </table>
      <p class="label">Invoiced net</p>
      <p class="figure"><%= InvoicedText %></p>
      <p class="label">Received net</p>
      <p class="figure"><%= ReceivedText %></p>
    <% } %>
  </div>
</body>
</html>
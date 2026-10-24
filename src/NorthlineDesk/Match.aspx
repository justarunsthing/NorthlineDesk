<%@ Page Language="C#" AutoEventWireup="true" Async="true" CodeBehind="Match.aspx.cs" Inherits="NorthlineDesk.MatchPage" %>
<!DOCTYPE html>
<html lang="en-GB">
<head>
  <meta charset="utf-8" />
  <title>Match <%= Server.HtmlEncode(InvoiceNumber) %></title>
  <link rel="icon" href="data:," />
  <link rel="stylesheet" href="site.css" />
</head>
<body>
  <div class="wrap">
    <header>
      <p class="mark">Northline desk</p>
      <h1>Invoice <%= Server.HtmlEncode(InvoiceNumber) %></h1>
    </header>
    <nav>
      <a href="Default.aspx">Desk</a>
      <a href="PurchaseOrder.aspx?po=PO-1001">PO-1001</a>
    </nav>
    <% if (HasError) { %>
      <p class="error"><%= Server.HtmlEncode(ErrorMessage) %></p>
    <% } else { %>
      <p class="path" data-decision-path="<%= Server.HtmlEncode(DecisionPathCode) %>">Decision path: <%= Server.HtmlEncode(DecisionPathLabel) %></p>
      <p class="label">Approved net</p>
      <p class="figure"><%= ApprovedText %></p>
      <p class="label">Unmatched callouts</p>
      <p class="figure"><%= Unmatched %></p>
      <p class="note"><%= Server.HtmlEncode(PathNote) %></p>
    <% } %>
  </div>
</body>
</html>
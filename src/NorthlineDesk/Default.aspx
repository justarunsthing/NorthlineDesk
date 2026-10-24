<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="NorthlineDesk.DefaultPage" %>
<!DOCTYPE html>
<html lang="en-GB">
<head>
  <meta charset="utf-8" />
  <title>Northline desk</title>
  <link rel="icon" href="data:," />
  <link rel="stylesheet" href="site.css" />
</head>
<body>
  <div class="wrap">
    <header>
      <p class="mark">Northline facilities</p>
      <h1>Reading depot</h1>
    </header>
    <p>Heston Mechanical invoiced twelve callouts on PO-1001. The depot received six. Open the match before anyone pays the invoice.</p>
    <nav>
      <a href="Match.aspx?invoice=INV-1001">Match INV-1001</a>
      <a href="PurchaseOrder.aspx?po=PO-1001">Purchase order PO-1001</a>
    </nav>
  </div>
</body>
</html>
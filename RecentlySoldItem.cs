
using System;

namespace DividendStockAdvisor;

class RecentlySoldItem
{
    public string Symbol {  get ; private set; }
    public DateTime BuyingAllowedDate { get; private set; }

    public RecentlySoldItem(string symbol, DateTime buyAllowedDate)
    {
        Symbol = symbol;
        BuyingAllowedDate = buyAllowedDate;
    }
}


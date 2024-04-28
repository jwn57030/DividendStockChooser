/*************************************************************************
* Description: Algorithms To Determine What Stocks to Sell and buy
* Author: Jason Neitzert
*************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;

namespace DividendStockAdvisor;

class StockChooser
{

    /******************************** Private Variables *****************/
    private InvestmentDatabase database = new();


    public const double DividendSellMultiplier = 1.30;  /* sell if totalgain is greater than dividend Percent times this amount */
    private const double SafetyPercent = .10; /* Safety percentage to round buy amounts by so I don't accidently spend more than I should */
    private const double MinStockPrice = 3.62;
    private const int MinDaysFromEXDateToSell = 5;

    private const double MaxDividendYield = 6.5;
    private const double MinDividendYield = 4.8;
    private const double AvailableToInvest = 437.21;
    private const double MaxAmountInvestedInStock = 100;
    private List<string> prohibitedList = ["ABEV", "ARKR", "AUBN", "BBVA", "BEP", "GLP", "HSQVY",
                                           "LRFC", "LX", "ORAN", "PFE", "PM", "SSRM", "STRW", "UBCP", "UNB", "UVV"];

    public List<InvestmentDatabaseItem>? itemsToSell { get; private set; }

    private readonly string _outputFolderPath = InvestmentDatabase._baseFileFolder + @"output\";  


    /********************** Public Properties **************************/
    public bool isReady
    {
        get
        {
            return database.databaseReady;
        }
    }

    /********************** Public Functions ***************************/

    /***************************************************************************
    * Description: Get Stocks To Sell, store them in RAM and CSVFile
    * Param: database - investmentdatabase to use
    * Return: Void
    ***************************************************************************/
    public void DetermineStocksToSell()
    {
        itemsToSell = database.Where(s =>
        {
            bool retval = false;

            /* Make sure I at least already own it before i try to sell it  */
            if (s.CurrentValue <= 0)
                retval = false;

            /* Make sure I sell any LP company to avoid multiple state taxes */
            else if ((s.Description.Contains(" LP", StringComparison.OrdinalIgnoreCase)) || (s.companyName.Contains(" LP", StringComparison.OrdinalIgnoreCase)))
            {
                retval = true;
            }

            /* Potentially Sell if TotalGain is significanly greater than CostBasisYield, or if Yield is below MinDividendYield */
            if ((s.CostBasisDividendYield > 0) && ((s.TotalGainLossPercent >= s.CostBasisDividendYield * DividendSellMultiplier) || s.CostBasisDividendYield < MinDividendYield))
            {
                /* if a dividend isn't pending go ahead and sell */
                if (s.exDate == null)
                    retval = true;
                else
                {
                    DateOnly today = DateOnly.FromDateTime(DateTime.Now);
                    var daysDiff = s.exDate?.DayNumber - today.DayNumber;

                    /* EXDate has passed and no more has been set so go ahead and sell */
                    if (daysDiff < 0)
                    {
                        retval = true;
                    }
                    /* exdate is further out, so just sell  */
                    else if (daysDiff > MinDaysFromEXDateToSell)
                    {
                        retval = true;
                    }
                }
            }

            return retval;
        }).ToList();

        /* Write results of items To Sell to A file */
        List<List<string>> sellItemsCSVData = new List<List<string>>();

        List<string> sellItemsHeader = ["Symbol", "Description", "Company Name", "Percent Gain", "Dividend Yield", "CostBasisYield", "Dividend SellPoint", "Amount Gained", "CostBasis", "Est Yearly Income"];
        sellItemsCSVData.Add(sellItemsHeader);

        foreach (var item in itemsToSell)
        {
            List<string> lineData = [item.symbol, item.Description, item.companyName, item.TotalGainLossPercent.ToString(),
                item.DividendYield.ToString(), Math.Round(item.CostBasisDividendYield, 2).ToString(), (item.CostBasisDividendYield * DividendSellMultiplier).ToString(), item.TotalGainLossDollar.ToString(),
                item.CostBasisTotal.ToString(), item.EstimatedAnnualIncome.ToString()];
            sellItemsCSVData.Add(lineData);
        }

        ExcelHelper.WriteCSVFile( _outputFolderPath + "sellItems.csv", sellItemsCSVData);
    }

    /***************************************************************************
    * Description: Take a given list of items and mark them as sold in the database
    * Param: itemsToSell - list of items to mark as sold
    * Return: Void
    ***************************************************************************/
    public void SellItems()
    {
        List<InvestmentDatabaseItem> updateItems = new();
        List<RecentlySoldItem> recentlySoldItems = new();

        foreach (InvestmentDatabaseItem item in itemsToSell)
        {
            var updateItem = database.Find(s => s.symbol == item.symbol);

            // Console.WriteLine("Found " + item.symbol + " at " + i);

            if (updateItem != null)
            {


                /* if item is sold and at risk of being a wash sale, add to list for tracking so we don't buy again in 30 days */
                if (updateItem.TotalGainLossPercent < 0 )
                {
                    DateTime buyAllowedData = DateTime.Today;

                    /* Calculate days to allow buying again to avoid wash sale rule */
                    buyAllowedData.AddDays(30 + 2);


                    recentlySoldItems.Add(new RecentlySoldItem(updateItem.symbol, DateTime.Today));
                }
                /* even if we aren't permantely saving the sale, make sure to save it was sold for current run,
                 *  so we don't buy and sell same stock on same day.  */
                updateItem.soldInLast30Days = true;
                updateItem.TotalGainLossPercent = 0;
                updateItem.TotalGainLossDollar = 0;
                updateItem.CurrentValue = 0;

                updateItems.Add(updateItem);
            }
        }

        database.UpdateInvestmentItems(updateItems);
        database.AddRecentlySoldItems(recentlySoldItems);
    }


    public void DetermineStocksToBuy()
    {
        /* Setup to saving items removed from consideration and why  */
        List<string> headers = ["Symbol",  "Reason", "Dividend", "Last Price", "Already Invested"];

        List<List<string>> RemovedItemsCSV = new List<List<string>>();
        RemovedItemsCSV.Add(headers);



        List<InvestmentDatabaseItem> buyItems = database.Where(s =>
        {
            bool retval = true;
            string reason = "";

            if (s.Dividend == 0)
            {
                retval = false;
                reason = "Zero Dividend";
            }
            else if (s.LastPrice == 0)
            {
                retval = false;
                reason = "Zero Price";
            }
            else if ((s.DividendYield > MaxDividendYield) || (s.DividendYield < MinDividendYield))
            {
                retval = false;
                reason = "Dividend Yield";
            }
            else if (s.Exchange == "OTC")
            {
                reason = "Exchange";
                retval = false;

            }
            else if (s.soldInLast30Days == true)
            {
                reason = "Sold In Last 30 Days";
                retval = false;
            }

            /* Make sure I dont buy  any LP company to avoid multiple state taxes */
            else if ((s.Description.Contains(" LP", StringComparison.OrdinalIgnoreCase)) || (s.companyName.Contains(" LP", StringComparison.OrdinalIgnoreCase))) 
            {
                reason = "Limited Partnership";
                retval = false;
            }
            else if (s.LastPrice < MinStockPrice)
            {
                retval = false;
                reason = "Min Stock Price";
            }
            else if (!((s.LastPrice * (1 + SafetyPercent) <= MaxAmountInvestedInStock - s.CurrentValue)))
            {
                retval = false;
                reason = "Max Amount Invested";
            }
            else if (!(AvailableToInvest >= (s.LastPrice * (1 + SafetyPercent))))
            {
                retval = false;
                reason = "Available To Invest";
            }
            else
            {
                /* check prohibited stock list */
                foreach (var item in prohibitedList)
                {
                    if (item == s.symbol)
                    {
                        reason = "Prohibited List";
                        retval = false;
                    }
                }
            }
                   

            if (retval == false)
            {
                /* if item was rejected save to removed list */
                List<string> list = new List<string>();
                list.Add(s.symbol);
                list.Add(reason);
                list.Add(s.Dividend.ToString());
                list.Add(s.LastPrice.ToString());
                list.Add(s.CurrentValue.ToString());
                RemovedItemsCSV.Add(list);
            }

            return retval;
        }   
        ).ToList();


        ExcelHelper.WriteCSVFile(_outputFolderPath +  "RemovedScreenerResults.csv", RemovedItemsCSV);

        /* sort Items to Buy by largest dividend */
        buyItems.Sort(InvestmentDatabaseItem.SortByDividend);

        /* Write Potential BuyItems to a List before further processing. For record before amounts are processed */
        List<List<string>> FilteredItemsToBuyCSV = new List<List<string>>();

        headers = ["Symbol", "Yield"];
        FilteredItemsToBuyCSV.Add(headers);
        foreach(var item in buyItems)
        {

            List<string> list = new List<string>();
            list.Add(item.symbol);
            list.Add(item.DividendYield.ToString());
            FilteredItemsToBuyCSV.Add(list);
        }

        ExcelHelper.WriteCSVFile(_outputFolderPath + "FilteredScreenerResults.csv", FilteredItemsToBuyCSV);


        /* Now calculate how much of each item to buy  */
        List<List<string>> BuyItemsCSV = new List<List<string>>();

        headers = ["Symbol", "Shares To Buy", "Amount Spent", "Running Total", "Yield", "Already Owned"];
        BuyItemsCSV.Add(headers);

        double totalAmountSpent = 0;
        double AmountWithSafetyValue = 0;
        double leftover = AvailableToInvest;

        int StocksToBuy = 0;

        foreach (var item in buyItems)
        {

            /* Caclculate number of shares to buy  */
            double maxToSpend = Math.Min(leftover, MaxAmountInvestedInStock - item.CurrentValue);
            double numberOfShares = Math.Floor(maxToSpend / (item.LastPrice * (1 + SafetyPercent)));
            double actualAmount = numberOfShares * item.LastPrice;
            double safetyAmount = numberOfShares * (item.LastPrice * (1 + SafetyPercent));


            totalAmountSpent += actualAmount;
            AmountWithSafetyValue += safetyAmount;

            leftover -= safetyAmount;



            if (numberOfShares > 0)
            {
                /* Save to list for saving to file */
                StocksToBuy++;
                List<string> list = new List<string>();
                list.Add(item.symbol);
                list.Add(numberOfShares.ToString());
                list.Add(safetyAmount.ToString());
                list.Add(AmountWithSafetyValue.ToString());
                list.Add(item.DividendYield.ToString());
                list.Add((item.CurrentValue > 0).ToString());
                BuyItemsCSV.Add(list);

                //Console.WriteLine(item);
                //Console.WriteLine(item.symbol + " Shares: " + numberOfShares.ToString() + " SafetyAmount: " + safetyAmount.ToString() + " TotalSpent: " + AmountWithSafetyValue.ToString());
            }
        }

        /* Print summary */
        Console.WriteLine("Actual Amount: " + totalAmountSpent + " Safety Value: " + AmountWithSafetyValue + " Leftover: " + leftover);
        Console.WriteLine("Number of Items: " + StocksToBuy);

        /* Save results to file  */
        ExcelHelper.WriteCSVFile(_outputFolderPath + "BuyItems.csv", BuyItemsCSV);

    }


        /***************************** Private Functions ***************************/
    private double GetPercentageSellPoint(InvestmentDatabaseItem item)
    {
        return item.CostBasisDividendYield * DividendSellMultiplier;

    }

}

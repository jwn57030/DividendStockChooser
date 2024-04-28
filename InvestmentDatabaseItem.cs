/*************************************************************************
* Description: A Single Investment Database Item
* Author: Jason Neitzert
*************************************************************************/
using System;


public class InvestmentDatabaseItem : IComparable<InvestmentDatabaseItem>
{
    /*************************  Public Class Variables ********************/
    public double AverageCostBasis;
    public double CostBasisTotal;
    public double CurrentValue;
    public double DividendYield;
    public double CostBasisDividendYield;
    public double LastPrice;
    public string symbol;
    public string companyName;
    public string securityType;
    public double TotalGainLossPercent;
    public double TotalGainLossDollar;
    public DateOnly? exDate;


    /* For filtering to get valid dividend stocks */
    public double Dividend;
    public double? DividendCoverageEPSNextYRByIAD;
    public double? DividendCoverageEPSTTMByIAD;
    public double? DividendGrowth5Yr;
    public double? DividendGrowthIADToPriorYR;
    public double? DividendLastQuarter;
    public double EstimatedAnnualIncome;
    public string Exchange;
    public bool soldInLast30Days;


    /* Saving but dont care for now */
    public string AccountName;
    public string AccountNumber;
    public string Description;
    public string type;

    /********************************** Public Functions **************************/

    /***************************************************************************
    * Description: Class Constructor
    ***************************************************************************/
    public InvestmentDatabaseItem()
    {
        /* Make sure all strings are empty  */
        symbol = "";
        companyName = "";
        securityType = "";
        Exchange = "";
        AccountName = "";
        Description = "";
        type = "";
        AccountNumber = "";
    }

    public InvestmentDatabaseItem Copy()
    {
        InvestmentDatabaseItem newCopy = new();

        newCopy.AverageCostBasis = AverageCostBasis;
        newCopy.CostBasisTotal = CostBasisTotal;
        newCopy.CurrentValue = CurrentValue;
        newCopy.DividendYield = DividendYield;
        newCopy.CostBasisDividendYield = CostBasisDividendYield;
        newCopy.LastPrice = LastPrice;
        newCopy.symbol = symbol;
        newCopy.companyName = companyName;
        newCopy.securityType = securityType;
        newCopy.TotalGainLossPercent = TotalGainLossPercent;
        newCopy.TotalGainLossDollar = TotalGainLossDollar;
        newCopy.exDate = exDate;


        /* For filtering to get valid dividend stocks */
        newCopy.Dividend = Dividend;
        newCopy.DividendCoverageEPSNextYRByIAD = DividendCoverageEPSNextYRByIAD;
        newCopy.DividendCoverageEPSTTMByIAD = DividendCoverageEPSTTMByIAD;
        newCopy.DividendGrowth5Yr = DividendGrowth5Yr;
        newCopy.DividendGrowthIADToPriorYR = DividendGrowthIADToPriorYR;
        newCopy.DividendLastQuarter = DividendLastQuarter;
        newCopy.EstimatedAnnualIncome = EstimatedAnnualIncome;
        newCopy.Exchange = Exchange;
        newCopy.soldInLast30Days = soldInLast30Days;


        /* Saving but dont care for now */
        newCopy.AccountName = AccountName;
        newCopy.AccountNumber = AccountNumber;
        newCopy.Description = Description;
        newCopy.type = type;

        return newCopy;
    }

    /***************************************************************************
    * Description: Outputs Item as a String
    * Return: String representation of InvestmentDatabaseItem
    ***************************************************************************/
    public override string ToString()
    {
        string output = $"{symbol} Price:{LastPrice} Amount:{CurrentValue} Change:{TotalGainLossPercent}% ${TotalGainLossDollar} Yield:{DividendYield}% ";

        return output;
    }

    /***************************************************************************
    * Description: Compare two InvestmentDatabaseItems to see which is greater
    * Param: compareItem - item to compare against
    * Return: int - 0 if equal, > 0 if greater, < 0 if less than
    ***************************************************************************/
    public int CompareTo(InvestmentDatabaseItem? compareItem)
    {
        if (compareItem == null)
            return -1;
        else
        {
            if (compareItem.DividendYield > DividendYield)
                return 1;
            else if (compareItem.DividendYield < DividendYield)
                return -1;
            else
                return symbol.CompareTo(compareItem.symbol);
        }
    }


    /***************************************************************************
    * Description: Sort Two Items by their Symbol Alphabetically
    * Param: x - first item to compare
    * Param: y - second item to compare
    * Return: int - 0, < 0 or > 0
    ***************************************************************************/
    public static int SortBySymbol(InvestmentDatabaseItem x, InvestmentDatabaseItem y)
    {
        return x.symbol.CompareTo(y.symbol);
    }

    /***************************************************************************
    * Description: Sort Two Items by their Dividend Percentage
    * Param: x - first item to compare
    * Param: y - second item to compare
    * Return: int - 0, < 0 or > 0
    ***************************************************************************/
    public static int SortByDividend(InvestmentDatabaseItem x, InvestmentDatabaseItem y)
    {
        if (y.DividendYield > x.DividendYield)
            return 1;

        if (y.DividendYield < x.DividendYield)
            return -1;

        return 0;
    }
}

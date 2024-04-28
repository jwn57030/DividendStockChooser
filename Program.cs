/*************************************************************************
* Description: Main Program Entry
* Author: Jason Neitzert
*************************************************************************/
using DividendStockAdvisor;
using System;

/******************************* Public Functions *************************/

/***************************************************************************
* Description: Main Entry Point
****************************************************************************/

  StockChooser chooser = new StockChooser();


if (!chooser.isReady)
{
    /* Error Occurred so just bail out  */
    return;
}

chooser.DetermineStocksToSell();

if (chooser.itemsToSell != null && chooser.itemsToSell.Count > 0)
{

    Console.WriteLine("Did You Sell The Items?");
    var response = Console.ReadLine();

    if (response != null)
    {
        if (response == "Y")
        {
            chooser.SellItems();
        }
    }
}

chooser.DetermineStocksToBuy();





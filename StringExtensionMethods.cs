/*************************************************************************
* Description: String Extension Methods
* Author: Jason Neitzert
*************************************************************************/

namespace ExtensionMethods;

public static class StringExtensions
{

    /******************************* Public Functions *************************/

    /***************************************************************************
    * Description: Remove all instances of a char from a string
    * Param: str - string to modify
    * Param: removeChar - charachter to remove
    * Return: New String
    ***************************************************************************/
    public static string RemoveChar(this string str, char removeChar)
    {
        string newString = "";

        foreach(var item in str)
        {
            if (item != removeChar)
            {
                newString += item;
            }
        }

        return newString;
    }

    /***************************************************************************
    * Description: Remove all instances of given chars from a string
    * Param: str - string to modify
    * Param: removeChars - charachters to remove
    * Return: New String
    ***************************************************************************/
    public static string RemoveChars(this string str, string removeChars)
    {
        string newString = "";

        foreach(var item in str)
        {
            bool matchFound = false;

            foreach(var match in removeChars)
            {
                if (match == item)
                {
                    matchFound = true;
                    break;
                }

            }

            if (!matchFound) 
            {
                newString += item;
            }
        }

        return newString;
    }

    /***************************************************************************
    * Description: See if string has any other charachters than one given
    * Param: str - string to check
    * Param: check - char used to verify if any other charachters exist than this one
    * Return: bool - true if anything but given char exists in string
    ***************************************************************************/
    public static bool ContainsOtherThan(this string str, char check)
    {
        bool foundOther = false;

        foreach(var item in str)
        {
            if (item != check)
            {
                foundOther = true;
                break;             
            }
        }

        return foundOther;
    }
}


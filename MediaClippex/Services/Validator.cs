using System.Text.RegularExpressions;

namespace MediaClippex.Services;

public static class Validator
{
    /// <summary>
    ///     Checks if a given string is a number.
    /// </summary>
    /// <param name="input">The string to be validated.</param>
    /// <param name="wholeNumber">Specify whether the number should be a whole number or not.</param>
    /// <returns>True if the string is a valid number based on the criteria, false otherwise.</returns>
    public static bool IsNumber(string input, bool wholeNumber = false)
    {
        return Regex.IsMatch(input, wholeNumber ? @"^\d+$" : @"^-?\d+(\.\d+)?$");
    }
}
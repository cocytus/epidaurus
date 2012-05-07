using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;

namespace Epidaurus.Domain.DataValidation
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class SpearatedListOfAttribute : ValidationAttribute
    {
        private char _separator;
        private string[] _validWords;
        public bool AllowEmpty { get; set; }
        public bool CleanWhitespace { get; set; }
        public bool CaseSensitive { get; set; }

        public SpearatedListOfAttribute(char separator, string[] validWords)
        {
            _separator = separator;
            _validWords = validWords;

            AllowEmpty = true;
            CleanWhitespace = false;
            CaseSensitive = true;
        }

        public override bool IsValid(object value)
        {
            if (AllowEmpty && value == null)
                return true;

            var str = value as string;
            if (str == null)
                return false;
            var elems = str.Split(_separator).Select(el => CleanWhitespace ? el.Trim() : el).Where(el => el.Length > 0).ToArray();

            if (elems.Length == 0 && !AllowEmpty)
                return false;

            if (!CaseSensitive)
            {
                elems = elems.Select(el=>el.ToLowerInvariant()).ToArray();
                _validWords = _validWords.Select(el => el.ToLowerInvariant()).ToArray();
            }

            return elems.All(el => _validWords.Contains(el));
        }

        public override string FormatErrorMessage(string name)
        {
            return "Invalid data.";
        }
    }
}
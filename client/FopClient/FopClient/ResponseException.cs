using System;
using System.Collections.Generic;
using System.Text;

namespace Flexerant.FopClient
{
    public class ResponseException : Exception
    {
        public ResponseException(string message) : base(message) { }
    }
}

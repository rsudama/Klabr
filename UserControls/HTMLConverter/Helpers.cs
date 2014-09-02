using System;

///
/// These assertion methods were added by Gemcom to facilitate debugging. Rather than having to
/// track down the source of an assertion, they all get directed here for common handling.
///
namespace HTMLConverter
{
    class Helpers
    {
        public static void Assert(bool condition)
        {
            if (!condition)
                throw new Exception("Unable to parse HTML document.");
            //Debug.Assert(condition);
        }

        public static void Assert(bool condition, string msg)
        {
            if (!condition)
                throw new Exception("Unable to parse HTML document: " + msg);
            //Debug.Assert(condition, msg);
        }
    }
}

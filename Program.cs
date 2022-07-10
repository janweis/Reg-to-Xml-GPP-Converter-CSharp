using CommandLine;
using System;

namespace Reg_To_XmlGpp
{
    internal class Program
    {
        static void Main(string[] args)
        {
            ParserResult<ArgumentOptions> parserResults = Parser.Default.ParseArguments<ArgumentOptions>(args);
            ArgumentOptions arguments = parserResults.Value;

            if (null != arguments)
            {
                RegHandler regHandler = new RegHandler(arguments.File, arguments.Output, arguments.Action);
                regHandler.Start();
            }
        }
    }
}

using CommandLine;

namespace Reg_To_XmlGpp
{
    internal class ArgumentOptions
    {
        [Option("file",
            Required = true,
            HelpText = "Input Regfile-Path.")]
        public string File { get; set; }

        [Option("output",
            Required = false,
            HelpText = "Output Xml-Path.")]
        public string Output { get; set; }

        [Option("action",
            Required = false,
            Default = ItemAction.Update,
            HelpText = "Set the default Action for the regkey. [Create/Replace/Update/Delete]")]
        public ItemAction Action { get; set; }

        [Option("debug",
            Required = false,
            Default = false,
            HelpText = "Show all debug informations if error occurs.")]
        public bool Debug { get; set; }
    }
}

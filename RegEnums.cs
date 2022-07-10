using System.ComponentModel;

namespace Reg_To_XmlGpp
{
    public enum ItemType
    {
        [DefaultValue("REG_SZ")]
        REG_SZ,

        [DefaultValue("REG_DWORD")]
        REG_DWORD,

        [DefaultValue("REG_EXPAND_SZ")]
        REG_EXPAND_SZ,

        [DefaultValue("REG_MULTI_SZ")]
        REG_MULTI_SZ,

        [DefaultValue("REG_QWORD")]
        REG_QWORD,

        [DefaultValue("REG_BINARY")]
        REG_BINARY,

        [DefaultValue("REG_NONE")]
        REG_NONE,

        [DefaultValue("")]
        OnlyRegKey
    }

    public enum ItemAction
    {
        [DefaultValue("C")]
        Create,

        [DefaultValue("U")]
        Update,

        [DefaultValue("R")]
        Replace,

        [DefaultValue("D")]
        Delete
    }
}

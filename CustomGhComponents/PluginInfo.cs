using Grasshopper.Kernel;
using System;
using System.Drawing;

namespace CustomGhComponents
{
    public class PluginInfo : GH_AssemblyInfo
    {
        public override string Name
        {
            get
            {
                return "CustomGhComponents";
            }
        }
        public override Bitmap Icon
        {
            get
            {
                //Return a 24x24 pixel bitmap to represent this GHA library.
                return null;
            }
        }
        public override string Description
        {
            get
            {
                //Return a short string describing the purpose of this GHA library.
                return "";
            }
        }
        public override Guid Id
        {
            get
            {
                return new Guid("a531a4c8-3875-43d8-97ca-3f4882d70c4b");
            }
        }

        public override string AuthorName
        {
            get
            {
                //Return a string identifying you or your company.
                return "";
            }
        }
        public override string AuthorContact
        {
            get
            {
                //Return a string representing your preferred contact details.
                return "";
            }
        }
    }
}

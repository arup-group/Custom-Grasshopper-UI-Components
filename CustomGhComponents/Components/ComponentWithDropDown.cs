using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace CustomGhComponents
{
    public class ComponentWithDropDown : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public ComponentWithDropDown()
          : base("DropDownComponent", "DropDown",
              "A component with dropdown menus",
              "Category", "Subcategory")
        {
        }

        //This region overrides the typical component layout
        public override void CreateAttributes()
        {
            if (first)
            {
                FunctionToSetSelectedContent(0, 0);
                first = false;
            }
            m_attributes = new CustomUI.DropDownUIAttributes(this, FunctionToSetSelectedContent, dropdowncontents, selections, spacerDescriptionText);
        }

        public void FunctionToSetSelectedContent(int dropdownListId, int selectedItemId)
        {
            // on first run we create the combined dropdown content
            if (dropdowncontents == null)
            {
                // create list to populate dropdown content with
                dropdowncontents = new List<List<string>>(); //clear all previous content
                selections = new List<string>();

                dropdowncontents.Add(dropdownTopLevelContent); //add Top Level content as first list
                selections.Add(dropdownTopLevelContent[0]);

                dropdowncontents.Add(dropdownLevel2_A_Content); //add level 2 first list as default on first run
                selections.Add(dropdownLevel2_A_Content[0]);

                // add the lists corrosponding to top level content order
                dropdownLevel2_Content.Add(dropdownLevel2_A_Content);
                dropdownLevel2_Content.Add(dropdownLevel2_B_Content);
                dropdownLevel2_Content.Add(dropdownLevel2_C_Content);
                dropdownLevel2_Content.Add(dropdownLevel2_D_Content);
            }

            if (dropdownListId == 0) // if change is made to first list
            {
                // change the content of level 2 based on selection
                dropdowncontents[1] = dropdownLevel2_Content[selectedItemId];

                // update the shown selected to first item in list
                selections[1] = dropdowncontents[1][0];
            }

            if (dropdownListId == 1) // if change is made to second list
            {
                selections[1] = dropdowncontents[1][selectedItemId];

                // do something with the selected item
                System.Windows.Forms.MessageBox.Show("You selected: " + dropdowncontents[1][selectedItemId]);
            }

            // for Grasshopper to redraw the component to get changes to dropdown menu displayed on canvas:
            Params.OnParametersChanged();
            ExpireSolution(true);
        }

        #region dropdownmenu content
        // this region is where (static) lists are created that will be displayed
        // in the dropdown menus dependent on user selection.

        List<List<string>> dropdowncontents; // list that holds all dropdown contents
        List<List<string>> dropdownLevel2_Content = new List<List<string>>(); // list to hold level2 content

        List<string> selections; // list of the selected items 
        bool first = true; // bool to create menu first time the component runs
        
        readonly List<string> spacerDescriptionText = new List<string>(new string[]
        {
            "TopLevel List",
            "Level2 Items"
        });
        readonly List<string> dropdownTopLevelContent = new List<string>(new string[]
        {
            "ListA",
            "ListB",
            "ListC",
            "ListD"
        });
        // lists longer than 10 will automatically get a vertical scroll bar
        readonly List<string> dropdownLevel2_A_Content = new List<string>(new string[]
        {
            "Item A1",
            "Item A2",
            "Item A3",
            "Item A4",
            "Item A5",
            "Item A6",
            "Item A7",
            "Item A8",
            "Item A9",
            "Item A10",
            "Item A11",
            "Item A12",
            "Item A13",
        });

        readonly List<string> dropdownLevel2_B_Content = new List<string>(new string[]
        {
            "Item B1",
            "Item B2",
            "Item B3",
            "Item B4",
            "Item B5",
            "Item B6",
            "Item B7",
            "Item B8",
            "Item B9",
        });

        readonly List<string> dropdownLevel2_C_Content = new List<string>(new string[]
        {
            "Item C1",
            "Item C2",
            "Item C3",
            "Item C4",
            "Item C5",
            "Item C6",
            "Item C7",
            "Item C8",
            "Item C9",
            "Item C10",
            "Item C11",
            "Item C12",
            "Item C13",
            "Item C14",
            "Item C15",
            "Item C16",
        });

        readonly List<string> dropdownLevel2_D_Content = new List<string>(new string[]
        {
            "Item D1",
            "Item D2",
            "Item D3",
            "Item D4",
            "Item D5",
            "Item D6",
        });
        #endregion

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Input1", "I1", "First Input", GH_ParamAccess.item);
            pManager[0].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Output1", "O1", "First Output", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // set output to selected
            DA.SetData(0, selections[1]);
        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                // You can add image files to your project resources and access them like this:
                //return Resources.IconForThisComponent;
                return null;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("020959db-8b03-4a62-9a25-5b34e4e44812"); }
        }
    }
}

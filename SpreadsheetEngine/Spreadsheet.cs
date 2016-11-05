// CptS 321
// Emily Clemens

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.IO;
using System.Xml;
using System.Windows.Forms;

namespace CptS322
{
    // spreadsheet internal implimentation of the abstract cell class
    class SheetCell : Cell
    {
        public string Name;
        public ExpTree ETree;
        public bool InvalidExp = false; // set when ETree contains bad/circular/self refs
        public HashSet<string> Deps = new HashSet<string>();

        public SheetCell(int rowIndex, int colIndex) : base(rowIndex, colIndex)
        {
            this.Name = Spreadsheet.RowCol2Name(rowIndex, colIndex);
        }

        public void SetValue(string v)
        {
            val = v;
        }
    }

    public class Spreadsheet
    {
        public Spreadsheet(int numRows, int numCols)
        {
            RowCount = numRows;
            ColumnCount = numCols;

            Cells = new SheetCell[numRows, numCols];
            // initialize each cell in the spreadsheet and handle the PropertyChanged
            // event for each one
            for (int r = 0; r < numRows; r++)
            {
                for (int c = 0; c < numCols; c++)
                {
                    Cells[r, c] = new SheetCell(r, c);
                    Cells[r, c].PropertyChanged += CellChanged;
                }
            }
        }

        public Cell GetCell(int row, int col)
        {
            try
            {
                return Cells[row, col];
            }
            catch
            {
                return null;
            }
        }

        // called whenever a cell changes so that we can dispatch another event as well as 
        // check to see if we need to look up the value in another cell
        private void CellChanged(object sender, PropertyChangedEventArgs e)
        {
            var cell = sender as SheetCell;

            if (e.PropertyName == "Color")
            {
                CellPropertyChanged(cell, new PropertyChangedEventArgs("Color"));
                return;
            }

            // reset expression validity when it changes
            // if its still invalid it will get set back
            // to true
            cell.InvalidExp = false;

            // handle raw numbers 
            double val;
            if (double.TryParse(cell.Text, out val))
            {
                foreach (var old in cell.Deps)
                    Refs[Name2Cell(old)].Remove(cell);

                cell.Deps.Clear();
                Vars[cell.Name] = val;
                cell.SetValue(val.ToString());
            }
            else if (cell.Text != null && cell.Text.Length > 0 && cell.Text[0] == '=')
            {
                cell.ETree = new ExpTree(cell.Text.Substring(1), Vars);

                double res = cell.ETree.Eval();
                var newDeps = cell.ETree.GetExpLocals();

                // cell expression variables contain reference to the cell itself
                if (newDeps.FirstOrDefault(dep => dep == cell.Name) != null)
                {
                    cell.SetValue("!(self reference)");
                    cell.InvalidExp = true;
                }
                // one of the cell expression variables is invalid
                else if (newDeps.FirstOrDefault(dep => Name2Cell(dep) == null) != null)
                {
                    cell.SetValue("!(bad reference)");
                    cell.InvalidExp = true;
                }
                // chain of cell exp vars contains a circular reference
                else if (newDeps.ToList().FirstOrDefault(p => ContainsCircularRef(cell, Name2Cell(p))) != null)
                {
                    cell.SetValue("!(circ reference)");
                    cell.InvalidExp = true;
                }
                else
                {
                    Vars[cell.Name] = res;

                    // remove dependencies that arent needed anymore
                    foreach (var old in cell.Deps.Except(newDeps))
                        Refs[Name2Cell(old)].Remove(cell);

                    cell.Deps = newDeps;

                    // add current cell to each set corresponding to one of the current cell's
                    // dependencies
                    foreach (var dep in cell.Deps)
                    {
                        var rcell = Name2Cell(dep);

                        if (!Refs.ContainsKey(rcell))
                            Refs.Add(rcell, new HashSet<SheetCell>());

                        Refs[rcell].Add(cell);
                    }

                    cell.SetValue(res.ToString());
                }
            }
            else
            {
                // remove previous dependencies when directly setting text
                foreach (var old in cell.Deps)
                    Refs[Name2Cell(old)].Remove(cell);

                // changing cells that have dependencies from numerical to be strings 
                // defaults the cells value to 0 so that all dependencies get updated with
                // the 0 value 
                Vars[cell.Name] = 0;
                cell.SetValue(cell.Text);
            }

            // now update all dependencies
            UpdateRefs(cell);
            CellPropertyChanged((sender as SheetCell), new PropertyChangedEventArgs("Value"));
        }

        private void UpdateRefs(SheetCell cell)
        {
            if (!Refs.ContainsKey(cell))
                return;

            // recompute the values and update Vars dict
            foreach (var refCell in Refs[cell])
            {
                if (refCell.InvalidExp)
                    continue; 

                var res = refCell.ETree.Eval();
                Vars[refCell.Name] = res;
                refCell.SetValue(res.ToString());
                CellPropertyChanged(refCell, new PropertyChangedEventArgs("Value"));

                // recursively update ref ref calls etc
                UpdateRefs(refCell);
            }
        }

        // check to see if a circ reference occurs if the parent were to add
        // the cell as a dependant
        private bool ContainsCircularRef(SheetCell cell, SheetCell parent)
        {
            var cellChain = new HashSet<SheetCell>();
            cellChain.Add(parent);

            return ContainsCircularRefRec(cell, cellChain);
        }

        private bool ContainsCircularRefRec(SheetCell cell, HashSet<SheetCell> curChain)
        {
            if (curChain.Contains(cell))
                return true;

            if (Refs.ContainsKey(cell) && Refs[cell].Count > 0)
            {
                curChain.Add(cell);

                foreach (var cellRef in Refs[cell])
                {
                    // create a duplicate HashSet for every new possible walk on the graph of cells.
                    // this is so that cells that reference multiple values that don't contain
                    // crefs wont cause false positives
                    if (ContainsCircularRefRec(cellRef, new HashSet<SheetCell>(curChain)))
                        return true;
                }
            }

            return false;
        }

        // convert string name like A4 or B20 into a Cell object
        private SheetCell Name2Cell(string name)
        {
            if (!char.IsLetter(name[0]))
                return null;

            int col = char.ToUpper(name[0]) - 65; // convert alphanumeric to index

            int row;
            bool isInt = Int32.TryParse(name.Substring(1), out row);
            if (!isInt || row > RowCount)
                return null;

            return Cells[row - 1, col];
        }

        // turn a row and column into an ascii name eg row=3,col=0 => A3
        public static string RowCol2Name(int row, int col)
        {
            string c = ((char)(col + 65)).ToString();
            return c + (row + 1);
        }

        public void AddUndo(UndoRedoCollection ur)
        {
            undos.Push(ur);
        }

        // exec the current popped undo and then pop it onto the redo stack
        public void DoUndo()
        {
            UndoRedoCollection redo = (UndoRedoCollection)undos.Pop().Exec();
            redos.Push(new UndoRedoCollection(redo.Type, redo));
        }

        public void DoRedo()
        {
            var undo = (UndoRedoCollection)redos.Pop().Exec();
            undos.Push(new UndoRedoCollection(undo.Type, undo));
        }

        public void SaveXML(Stream xmls)
        {
            using (XmlWriter writer = XmlWriter.Create(xmls))
            {
                writer.WriteStartElement("spreadsheet");
                foreach (var cell in Cells)
                {
                    if (cell.CellChanged)
                    {
                        writer.WriteStartElement("cell");

                        writer.WriteElementString("id", RowCol2Name(cell.RowIndex, cell.ColumnIndex));
                        writer.WriteElementString("bg", cell.BGColor.ToString("X"));
                        writer.WriteElementString("text", cell.Text);

                        writer.WriteEndElement();
                    }
                }
                writer.WriteEndElement();
            }
        }

        public void LoadXML(Stream xmls)
        {
            XmlDocument xmld = new XmlDocument();
            xmld.Load(xmls);

            foreach (XmlNode cells in xmld.GetElementsByTagName("cell"))
            {
                SheetCell curCell = null;
                string curText = null;
                uint curBG = 0xFFFFFFFF;

                foreach (XmlNode cell in cells.ChildNodes)
                {
                    if (cell.Name == "id")
                        curCell = Name2Cell(cell.InnerText);
                    else if (cell.Name == "bg")
                        curBG = Convert.ToUInt32(cell.InnerText, 16);
                    else if (cell.Name == "text")
                        curText = cell.InnerText;
                }

                if (curCell != null)
                {
                    curCell.BGColor = curBG;
                    curCell.Text = curText;
                }
            }
        }

        public void ResetState()
        {
            foreach (var c in Cells)
            {
                c.Text = null;
                c.BGColor = 0xFFFFFFFF;
            }

            undos.Clear();
            redos.Clear();
        }

        public event PropertyChangedEventHandler CellPropertyChanged = delegate { };
        private SheetCell[,] Cells;

        private int RowCount { get; }
        private int ColumnCount { get; }

        private Dictionary<string, double> Vars = new Dictionary<string, double>();
        private Dictionary<SheetCell, HashSet<SheetCell>> Refs = new Dictionary<SheetCell, HashSet<SheetCell>>();

        private Stack<UndoRedoCollection> undos = new Stack<UndoRedoCollection>();
        private Stack<UndoRedoCollection> redos = new Stack<UndoRedoCollection>();

        // peek at the current next undo to be applied
        public UndoRedoCollection NextUndo
        {
            get
            {
                return UndosEmpty ? null : undos.Peek();
            }
        }
        // peek at next redo to be applied
        public UndoRedoCollection NextRedo
        {
            get
            {
                return RedosEmpty ? null : redos.Peek();
            }
        }

        public bool UndosEmpty
        {
            get
            {
                return undos.Count == 0;
            }
        }
        public bool RedosEmpty
        {
            get
            {
                return redos.Count == 0;
            }
        }
    }
}

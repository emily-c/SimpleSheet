// CptS 321
// Emily Clemens

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CptS322;

namespace Spreadsheet_EClemens
{
    public partial class Form1 : Form
    {
        private Spreadsheet Sheet = new Spreadsheet(50, 26);
        private ColorDialog CDialog = new ColorDialog();

        public Form1()
        {
            InitializeComponent();
            Sheet.CellPropertyChanged += Sheet_CellPropertyChanged;
        }

        private void Sheet_CellPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var cell = sender as Cell;

            if (e.PropertyName == "Value")
                dataGridView1[cell.ColumnIndex, cell.RowIndex].Value = cell.Value;
            else if (e.PropertyName == "Text")
                dataGridView1[cell.ColumnIndex, cell.RowIndex].Value = cell.Text;
            else if (e.PropertyName == "Color")
                dataGridView1[cell.ColumnIndex, cell.RowIndex].Style.BackColor = Color.FromArgb(cell.BGColor != 0 ? (int)cell.BGColor : -1);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            const string cols = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

            for (int i = 0; i < 26; i++)
            {
                var cname = cols[i].ToString();
                dataGridView1.Columns.Add("col" + cname, cname);
            }

            // set each row index to the right number and then resize to fit correctly
            dataGridView1.Rows.Add(50);
            foreach (DataGridViewRow r in dataGridView1.Rows)
            {
                r.HeaderCell.Value = (r.Index + 1).ToString();
            }
            dataGridView1.AutoResizeRowHeadersWidth(DataGridViewRowHeadersWidthSizeMode.AutoSizeToAllHeaders);
        }

        private void dataGridView1_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            // during edit show text
            dataGridView1[e.ColumnIndex, e.RowIndex].Value = Sheet.GetCell(e.RowIndex, e.ColumnIndex).Text;
        }

        private void dataGridView1_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            // if expression then show value
            var val = dataGridView1[e.ColumnIndex, e.RowIndex].Value;
            if (val != null)
            {
                Cell cell = Sheet.GetCell(e.RowIndex, e.ColumnIndex);
                Sheet.AddUndo(new UndoRedoCollection(UndoRedoType.Text, new UndoRedoText(cell, cell.Text)));

                cell.Text = val.ToString();
                dataGridView1[e.ColumnIndex, e.RowIndex].Value = cell.Value;
            }

            updateUndoRedoMenuItems();
        }

        private void backgroundColorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (CDialog.ShowDialog() == DialogResult.OK)
            {
                var undos = new UndoRedoCollection(UndoRedoType.Color);

                foreach (DataGridViewCell dgCell in dataGridView1.SelectedCells)
                {
                    var cell = Sheet.GetCell(dgCell.RowIndex, dgCell.ColumnIndex);
                    undos.Add(new UndoRedoColor(cell, cell.BGColor));

                    cell.BGColor = (uint)CDialog.Color.ToArgb();
                }

                Sheet.AddUndo(undos);
            }

            updateUndoRedoMenuItems();
        }

        private void undoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Sheet.DoUndo();
            updateUndoRedoMenuItems();
        }

        private void redoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Sheet.DoRedo();
            updateUndoRedoMenuItems();
        }

        private void updateUndoRedoMenuItems()
        {
            redoToolStripMenuItem.Enabled = true;
            undoToolStripMenuItem.Enabled = true;

            if (Sheet.UndosEmpty)
            {
                undoToolStripMenuItem.Text = "Undo";
                undoToolStripMenuItem.Enabled = false;
            }
            if (Sheet.RedosEmpty)
            {
                redoToolStripMenuItem.Text = "Redo";
                redoToolStripMenuItem.Enabled = false;
            }

            // adjust the text depending on the type of undo/redo operation to be performed
            var nu = Sheet.NextUndo;
            var nr = Sheet.NextRedo;

            if (nu != null)
            {
                if (nu.Type == UndoRedoType.Text)
                {
                    undoToolStripMenuItem.Text = "Undo cell text change";
                }
                else if (nu.Type == UndoRedoType.Color)
                {
                    undoToolStripMenuItem.Text = string.Format("Undo changing cell{0} background color{0}", nu.UndoRedoCount > 1 ? "s" : "");
                }
            }
            if (nr != null)
            {
                if (nr.Type == UndoRedoType.Text)
                {
                    redoToolStripMenuItem.Text = "Redo cell text change";
                }
                else if (nr.Type == UndoRedoType.Color)
                {
                    redoToolStripMenuItem.Text = string.Format("Redo changing cell{0} background color{0}", nr.UndoRedoCount > 1 ? "s" : "");
                }
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var sfd = new SaveFileDialog();
            sfd.Filter = "XML Files (*.xml)|*.xml";
            sfd.DefaultExt = "xml";
            sfd.AddExtension = true;

            if (sfd.ShowDialog() == DialogResult.OK)
                using (var fs = sfd.OpenFile())
                    Sheet.SaveXML(fs);
        }

        private void loadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var ofd = new OpenFileDialog();
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                Sheet.ResetState();
                dataGridView1.ClearSelection();

                using (var fs = ofd.OpenFile())
                    Sheet.LoadXML(fs);
            }
        }

        private void resetSheetToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Sheet.ResetState();
            dataGridView1.ClearSelection();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}

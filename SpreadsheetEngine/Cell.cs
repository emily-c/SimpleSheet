// CptS 321
// Emily Clemens

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

namespace CptS322
{
    abstract public class Cell : INotifyPropertyChanged
    {
        public Cell(int rowIndex, int colIndex)
        {
            row = rowIndex;
            col = colIndex;
            color = 0xFFFFFFFF;
            CellChanged = false;
        }

        public bool CellChanged;
        public int RowIndex { get { return row; } }
        public int ColumnIndex { get { return col; } }
        public string Value
        {
            get
            {
                return val;
            }
        }
        public string Text
        {
            get { return text; }
            set
            {
                if (value != text)
                {
                    text = value;
                    CellChanged = true;
                    PropertyChanged(this, new PropertyChangedEventArgs("Text"));
                }
            }
        }
        // for cell background color
        public uint BGColor
        {
            get { return color; }
            set
            {
                if (color != value)
                {
                    color = value;
                    CellChanged = true;
                    PropertyChanged(this, new PropertyChangedEventArgs("Color"));
                }
            }
        }

        protected string text, val;
        protected uint color;
        protected int row, col;
        public event PropertyChangedEventHandler PropertyChanged;
    }
}

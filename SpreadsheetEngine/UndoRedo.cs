// CptS 321
// Emily Clemens

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CptS322
{
    public interface IUndoRedo
    {
        IUndoRedo Exec();
    }

    // text undo/redo command returning the old text value
    public class UndoRedoText : IUndoRedo
    {
        public UndoRedoText(Cell cell, string text)
        {
            this.cell = cell;
            prevText = text;
        }

        public IUndoRedo Exec()
        {
            string curText = cell.Text;
            cell.Text = prevText;
            return new UndoRedoText(cell, curText);
        }

        private Cell cell;
        string prevText;
    }

    // color undo/redo command returning the old color value
    public class UndoRedoColor : IUndoRedo
    {
        public UndoRedoColor(Cell cell, uint color)
        {
            this.cell = cell;
            prevColor = color;
        }

        public IUndoRedo Exec()
        {
            uint curColor = cell.BGColor;
            cell.BGColor = prevColor;
            return new UndoRedoColor(cell, curColor);
        }

        private Cell cell;
        private uint prevColor;
    }

    public enum UndoRedoType { Text, Color };

    // multiple undos/redos
    public class UndoRedoCollection : IUndoRedo
    {
        public UndoRedoCollection(UndoRedoType type) { Type = type; }

        // make it easier to add a single undo to a collection right away
        public UndoRedoCollection(UndoRedoType type, IUndoRedo ur)
        {
            Type = type;
            Add(ur);
        }

        public void Add(IUndoRedo ur)
        {
            undoRedos.Add(ur);
        }

        public IUndoRedo Exec()
        {
            var undoList = new UndoRedoCollection(Type);

            // add the inverse operation of every element to the undoList to be
            // returned and possibly [un/re]done later
            foreach (var ur in undoRedos)
            {
                undoList.Add(ur.Exec());
            }

            return undoList;
        }

        private List<IUndoRedo> undoRedos = new List<IUndoRedo>();

        public UndoRedoType Type;
        public int UndoRedoCount
        {
            get
            {
                return undoRedos.Count;
            }
        }
    }
}

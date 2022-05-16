using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DtronixCommon.Collections.Trees;

public class LGridQuery4
{
    public SmallList<int>[] elements = new SmallList<int>[4];
}

public class LGridElt
{
    // Stores the index to the next element in the loose cell using an indexed SLL.
    public int next;

    // Stores the ID of the element. This can be used to associate external
    // data to the element.
    public int id;

    // Stores the center of the element.
    public float mx;
    public float my;

    // Stores the half-size of the element relative to the upper-left corner
    // of the grid.
    public float hx;
    public float hy;
}

public class LGridLooseCell
{
    // Stores the extents of the grid cell relative to the upper-left corner
    // of the grid which expands and shrinks with the elements inserted and 
    // removed.
    public float[] rect;

    // Stores the index to the first element using an indexed SLL.
    public int head;
}

public class LGridLoose
{
    // Stores all the cells in the loose grid.
    public LGridLooseCell[] cells;

    // Stores the number of columns, rows, and cells in the loose grid.
    public int num_cols, num_rows, num_cells;

    // Stores the inverse size of a loose cell.
    public float inv_cell_w, inv_cell_h;
}

public class LGridTightCell
{
    // Stores the index to the next loose cell in the grid cell.
    public int next;

    // Stores the position of the loose cell in the grid.
    public int lcell;
};

public class LGridTight
{
    // Stores all the tight cell nodes in the grid.
    public FreeList<LGridTightCell> cells;

    // Stores the tight cell heads.
    public int[] heads;

    // Stores the number of columns, rows, and cells in the tight grid.
    public int num_cols, num_rows, num_cells;

    // Stores the inverse size of a tight cell.
    public float inv_cell_w, inv_cell_h;
};

public class LGrid
{
    // Stores the tight cell data for the grid.
    public LGridTight tight;

    // Stores the loose cell data for the grid.
    public LGridLoose loose;

    // Stores all the elements in the grid.
    public FreeList<LGridElt> elts;

    // Stores the number of elements in the grid.
    public int num_elts;

    // Stores the upper-left corner of the grid.
    public float x, y;

    // Stores the size of the grid.
    public float w, h;
};


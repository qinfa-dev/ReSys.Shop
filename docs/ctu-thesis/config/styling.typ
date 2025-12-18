// Styling for Headings, Figures, Tables
#let setup-styling() = {
  // Chapter headings (Level 1)
  show heading.where(level: 1): it => {
    set align(center)
    set text(size: 14pt, weight: "bold")
    pagebreak(weak: true)
    v(12pt)
    upper(it.body)
    v(6pt)
  }

  // Section headings (Level 2)
  show heading.where(level: 2): it => {
    set text(size: 13pt, weight: "bold")
    v(3pt)
    it
    v(3pt)
  }

  // Subsection headings (Level 3)
  show heading.where(level: 3): it => {
    set text(size: 13pt, weight: "bold")
    v(3pt)
    it
    v(3pt)
  }

  // Figure settings
  set figure(supplement: [Figure])
  show figure.where(kind: table): set figure(supplement: [Table])
  
  // Table styling - no vertical lines, custom horizontal lines
  set table(
    stroke: (x, y) => {
      if y == 0 { // Top border of the table (for the header row)
        return (top: 1pt, bottom: 1pt) // And bottom border of the header row
      } else if y == table.rows - 1 { // Bottom border of the last row
        return (bottom: 1pt)
      }
      return (top: 0pt, bottom: 0pt) // No other horizontal lines
    },
    inset: 6pt,
  )

  // Table content font size
  show table.cell: set text(size: 10pt)

  // Figure caption styling
  show figure.caption: it => {
    set text(size: 13pt, weight: "bold")
    if it.kind == image {
      align(center, it)
    } else if it.kind == table {
      set align(left)
      pad(left: 1cm, it)
    } else {
      it
    }
  }

  // Table caption position
  show figure.where(kind: table): set figure.caption(position: top)

  // Equation numbering
  set math.equation(numbering: "(1)")
}
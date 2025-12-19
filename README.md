# CTU Undergraduate Thesis - Complete Project Structure

## 📁 Project Directory Structure

```
ctu-thesis/
├── main.typ                          # Main compilation file
├── config/
│   ├── page-setup.typ               # Page margins, headers, footers
│   ├── text-setup.typ               # Font, paragraph settings
│   └── styling.typ                  # Headings, figures, tables styles
├── frontmatter/
│   ├── cover.typ                    # Main cover page
│   ├── inner-cover.typ              # Inner cover with advisor info
│   ├── approval.typ                 # Approval page
│   ├── acknowledgments.typ          # Acknowledgments
│   ├── abstract-vi.typ              # Vietnamese abstract
│   ├── abstract-en.typ              # English abstract
│   ├── declaration.typ              # Student declaration
│   ├── table-of-contents.typ        # Auto-generated TOC
│   ├── list-of-tables.typ           # Auto-generated list
│   ├── list-of-figures.typ          # Auto-generated list
│   └── abbreviations.typ            # List of abbreviations
├── chapters/
│   ├── chapter1-introduction.typ    # Introduction chapter
│   ├── chapter2-literature.typ      # Literature review
│   ├── chapter3-methodology.typ     # Methodology
│   ├── chapter4-results.typ         # Results and discussion
│   └── chapter5-conclusion.typ      # Conclusion
├── backmatter/
│   ├── references.typ               # References section
│   └── appendices.typ               # Appendices
├── assets/
│   ├── images/
│   │   ├── logo.png                 # CTU logo
│   │   ├── architecture.png         # System architecture
│   │   └── screenshots/             # Application screenshots
│   └── data/
│       └── student-info.yml         # Student information
└── README.md                         # Project documentation
```

---

## 📄 File Contents

### **main.typ** - Main Compilation File
```typst
// Main Thesis Document
// Can Tho University - College of ICT
// Compile: typst compile main.typ

// Import configurations
#import "config/page-setup.typ": *
#import "config/text-setup.typ": *
#import "config/styling.typ": *

// Initialize page setup
#setup-page()
#setup-text()
#setup-styling()

// Front Matter
#include "frontmatter/cover.typ"
#include "frontmatter/inner-cover.typ"
#include "frontmatter/approval.typ"

// Start Roman numerals
#set page(numbering: "i")
#counter(page).update(1)

#include "frontmatter/acknowledgments.typ"
#include "frontmatter/abstract-vi.typ"
#include "frontmatter/abstract-en.typ"
#include "frontmatter/declaration.typ"
#include "frontmatter/table-of-contents.typ"
#include "frontmatter/list-of-tables.typ"
#include "frontmatter/list-of-figures.typ"
#include "frontmatter/abbreviations.typ"

// Main Content - Start Arabic numerals
#set page(numbering: "1")
#counter(page).update(1)
#set heading(numbering: "1.1.1.1")

#include "chapters/chapter1-introduction.typ"
#include "chapters/chapter2-literature.typ"
#include "chapters/chapter3-methodology.typ"
#include "chapters/chapter4-results.typ"
#include "chapters/chapter5-conclusion.typ"

// Back Matter
#include "backmatter/references.typ"
#include "backmatter/appendices.typ"
```

---

### **config/page-setup.typ** - Page Configuration
```typst
// Page Setup Configuration
#let setup-page() = {
  // Define header
  let thesis-header = locate(loc => {
    let page-num = counter(page).at(loc).first()
    if page-num > 0 {
      set text(size: 9pt)
      grid(
        columns: (1fr, 1fr),
        align: (left, right),
        [Graduation Thesis Academic Year 2023-2024],
        [Can Tho University]
      )
    }
  })

  // Define footer
  let thesis-footer = locate(loc => {
    let page-num = counter(page).at(loc).first()
    if page-num > 0 {
      set text(size: 9pt)
      grid(
        columns: (1fr, 1fr),
        align: (left, right),
        [Information Technology],
        [College of ICT]
      )
    }
  })

  // Apply page settings
  set page(
    paper: "a4",
    margin: (left: 3cm, right: 2cm, top: 2cm, bottom: 2cm),
    header-ascent: 1cm,
    footer-descent: 1cm,
    header: thesis-header,
    footer: thesis-footer,
    numbering: none,
  )
}
```

---

### **config/text-setup.typ** - Text Configuration
```typst
// Text and Paragraph Setup
#let setup-text() = {
  // Font settings
  set text(
    font: "Times New Roman",
    size: 13pt,
    lang: "en",
  )

  // Paragraph settings - Line spacing 1.5
  set par(
    leading: 0.78em,
    first-line-indent: 1cm,
    justify: true,
    spacing: 0.78em,
  )
}
```

---

### **config/styling.typ** - Styling Configuration
```typst
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
  
  // Table styling - no vertical lines
  set table(
    stroke: (x, y) => (
      top: if y <= 1 { 1pt } else { 0pt },
      bottom: 1pt,
    ),
    inset: 6pt,
  )

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
```

---

### **frontmatter/cover.typ** - Cover Page with Blue Border
```typst
// Main Cover Page with Blue Border Box
#page(
  margin: (left: 3cm, right: 3cm, top: 2.5cm, bottom: 2.5cm),
  numbering: none,
  header: none,
  footer: none,
)[
  // Blue border box surrounding the entire content
  #rect(
    width: 100%,
    height: 100%,
    stroke: rgb(0, 51, 153) + 2pt, // Blue color with 2pt thickness
    inset: 1.5cm,
  )[
    #set align(center)
    #set par(leading: 0.65em, spacing: 0.65em)
    
    #v(0.5cm)
    
    #text(size: 13pt, weight: "bold")[
      BỘ GIÁO DỤC VÀ ĐÀO TẠO\n      ĐẠI HỌC CẦN THƠ\n      TRƯỜNG ĐẠI HỌC CÔNG NGHỆ THÔNG TIN VÀ TRUYỀN THÔNG
    ]
    
    #v(1.5cm)
    
    // Add logo here if available
    // #image("assets/images/logo.png", width: 3cm)
    
    #v(1cm)
    
    #text(size: 14pt, weight: "bold")[
      LUẬN VĂN TỐT NGHIỆP\n      NGÀNH CÔNG NGHỆ THÔNG TIN
    ]
    
    #v(2cm)
    
    #text(size: 13pt, weight: "bold")[Đề tài:]
    
    #v(0.5cm)
    
    #text(size: 18pt, weight: "bold")[
      HỆ THỐNG PHẦN MỀM QUẢN LÝ\n      VĂN PHÒNG LUẬT SƯ
    ]
    
    #v(0.5cm)
    
    #text(size: 14pt, weight: "bold", style: "italic")[
      (MANAGEMENT SOFTWARE SYSTEM FOR LAW OFFICES)
    ]
    
    #v(2cm)
    
    #set align(left)
    #pad(left: 25%)[
      #grid(
        columns: (auto, auto),
        column-gutter: 1cm,
        row-gutter: 0.5cm,
        align: (right, left),
        
        text(size: 13pt)[*Sinh viên thực hiện*], [],
        text(size: 13pt)[Họ tên:], text(size: 13pt)[Trần Thị Kim Linh],
        text(size: 13pt)[Mã số:], text(size: 13pt)[B1910402],
        text(size: 13pt)[Khóa:], text(size: 13pt)[45],
      )
    ]
    
    #v(1cm)
    
    #set align(center)
    #text(size: 13pt)[Cần Thơ, 5/2023]
    
    #v(0.5cm)
  ]
]
```

---

## 📄 **frontmatter/inner-cover.typ** - Inner Cover with Blue Border
```typst
// Inner Cover Page with Blue Border Box and Advisor Information
#page(
  margin: (left: 3cm, right: 3cm, top: 2.5cm, bottom: 2.5cm),
  numbering: none,
  header: none,
  footer: none,
)[
  // Blue border box surrounding the entire content
  #rect(
    width: 100%,
    height: 100%,
    stroke: rgb(0, 51, 153) + 2pt, // Blue color with 2pt thickness
    inset: 1.5cm,
  )[
    #set align(center)
    #set par(leading: 0.65em, spacing: 0.65em)
    
    #v(0.5cm)
    
    #text(size: 13pt, weight: "bold")[
      BỘ GIÁO DỤC VÀ ĐÀO TẠO\n      ĐẠI HỌC CẦN THƠ\n      TRƯỜNG ĐẠI HỌC CÔNG NGHỆ THÔNG TIN VÀ TRUYỀN THÔNG
    ]
    
    #v(1.5cm)
    
    // Add logo here if available
    // #image("assets/images/logo.png", width: 3cm)
    
    #v(1cm)
    
    #text(size: 14pt, weight: "bold")[
      LUẬN VĂN TỐT NGHIỆP\n      NGÀNH CÔNG NGHỆ THÔNG TIN
    ]
    
    #v(1.5cm)
    
    #text(size: 13pt, weight: "bold")[Đề tài:]
    
    #v(0.5cm)
    
    #text(size: 18pt, weight: "bold")[
      HỆ THỐNG PHẦN MỀM QUẢN LÝ\n      VĂN PHÒNG LUẬT SƯ
    ]
    
    #v(0.5cm)
    
    #text(size: 14pt, weight: "bold", style: "italic")[
      (MANAGEMENT SOFTWARE SYSTEM FOR LAW OFFICES)
    ]
    
    #v(1.5cm)
    
    #set align(left)
    #pad(left: 20%)[
      #grid(
        columns: (auto, auto),
        column-gutter: 1.5cm,
        row-gutter: 0.4cm,
        align: (right, left),
        
        text(size: 13pt)[*Giảng viên hướng dẫn:*], [],
        [], text(size: 13pt)[TS. Thái Minh Tuấn],
        [],
        [],
        text(size: 13pt)[*Sinh viên thực hiện*], [],
        text(size: 13pt)[Họ tên:], text(size: 13pt)[Trần Thị Kim Linh],
        text(size: 13pt)[Mã số:], text(size: 13pt)[B1910402],
        text(size: 13pt)[Khóa:], text(size: 13pt)[45],
      )
    ]
    
    #v(1cm)
    
    #set align(center)
    #text(size: 13pt)[Cần Thơ, 5/2023]
    
    #v(0.5cm)
  ]
]
```

---

## 📄 **NEW: frontmatter/abbreviations.typ** - Using Variables
```typst
// List of Abbreviations - Using Variables
#import "../config/variables.typ": *

#page[
  #set align(center)
  #text(size: 14pt, weight: "bold")[DANH MỤC TỪ VIẾT TẮT]
  
  #v(1cm)
  
  #set align(left)
  #set par(first-line-indent: 0cm)
  
  // Display abbreviations from variables
  #for abbr in abbreviations [
    #grid(
      columns: (3cm, 1fr),
      gutter: 1cm,
      text(weight: "bold")[#abbr.at(0)],
      [#abbr.at(1) #if abbr.at(2) != "" [(#abbr.at(2))]]
    )
    #v(0.3cm)
  ]
]
```
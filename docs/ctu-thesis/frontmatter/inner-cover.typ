// Inner Cover Page with Blue Border - Using Variables
#import "../config/variables.typ": *

#page(
  margin: (left: 3cm, right: 3cm, top: 2.5cm, bottom: 2.5cm),
  numbering: none,
  header: none,
  footer: none,
)[
  // Blue border box
  #rect(
    width: 100%,
    height: 100%,
    stroke: doc_settings.border-color + doc_settings.border-thickness,
    inset: doc_settings.border-inset,
  )[
    #set align(center)
    #set par(leading: 0.65em, spacing: 0.65em)

    #v(0.5cm)

    // University Header
    #text(size: 13pt, weight: "bold")[
      #upper(university.ministry-en)\
      #upper(university.name-en)\
      #upper(university.college-en)
    ]

    #v(1cm)

    // Logo
    #image("../assets/images/logo.png", width: 2.5cm)

    #v(1cm)

    // Thesis Type
    #text(size: 14pt, weight: "bold")[
      #upper(thesis.type-en)\
      #upper(" " + student.major-en)
    ]

    #v(2cm)

    // Thesis Title
    #text(size: 18pt, weight: "bold")[
      #upper(thesis.title-en)
    ]

    #v(2cm)

    // Advisor and Student Information
    #set align(center)
    #pad(left: 10%)[
      #grid(
        columns: (auto, auto),
        column-gutter: 1.5cm,
        row-gutter: 0.4cm,
        align: (right, left),
        text(size: 13pt)[*Student:*], text(size: 13pt)[#student.full-name-en],
        text(size: 13pt)[*Student ID:*], text(size: 13pt)[#student.student-id],
        text(size: 13pt)[*Class:*], text(size: 13pt)[#student.cohort-year (K#student.cohort)],
        text(size: 13pt)[*Advisor:*], text(size: 13pt)[#get_advisor_full_name(lang: "en")],
      )
    ]

    #v(1cm)

    // Submission Date
    #set align(center)
    #text(size: 13pt)[#get_submission_date("en")]

    #v(0.5cm)
  ]
]

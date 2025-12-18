// ============================================================================
// STUDENT DECLARATION
// ============================================================================
#import "../config/variables.typ": *

#page[
  #set align(center)
  #text(size: 14pt, weight: "bold")[STUDENT DECLARATION]

  #v(1cm)

  #set align(left)
  #par(first-line-indent: 0cm)[
    I hereby declare that this thesis, entitled *#get-thesis-title(lang: "en")*,
    is my own original work and has not been submitted, either in whole or in part,
    for any other degree or qualification at this or any other institution.
    All sources of information used in this thesis have been properly acknowledged.
  ]

  #v(2.5cm)

  #align(right)[
    _#get_submission_date("en")_

    #v(1.5cm)

    #text(weight: "bold")[#student.full-name-en]
  ]
]

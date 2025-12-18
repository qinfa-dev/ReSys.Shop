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
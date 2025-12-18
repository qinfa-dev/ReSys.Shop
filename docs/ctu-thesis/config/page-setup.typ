// Page Setup Configuration
#let setup-page() = {
  // Define header
  // Apply page settings
  set page(
    paper: "a4",
    margin: (left: 3cm, right: 2cm, top: 2cm, bottom: 2cm),
    header-ascent: 1cm,
    footer-descent: 1cm,
    header: context [
      #locate(loc => {
        let page-num = counter(page).at(loc).first()
        if page-num > 0 {
          set text(size: 9pt)
          grid(
            columns: (1fr, 1fr),
            align: (left, right),
            [Graduation Thesis Academic Year 2023-2024],
            [Can Tho University]
          )
        } else {
          none
        }
      })
    ],
    footer: context [
      #locate(loc => {
        let page-num = counter(page).at(loc).first()
        if page-num > 0 {
          set text(size: 9pt)
          grid(
            columns: (1fr, 1fr),
            align: (left, right),
            [Information Technology],
            [College of ICT]
          )
        } else {
          none
        }
      })
    ],
    numbering: none,
  )
}
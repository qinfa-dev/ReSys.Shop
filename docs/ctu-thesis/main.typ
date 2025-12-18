// Main Thesis Document
// Can Tho University - College of ICT
// Compile: typst compile main.typ

// ============================================================================
// IMPORT CONFIGURATIONS AND VARIABLES
// ============================================================================

#import "config/variables.typ": *
#import "config/page-setup.typ": *
#import "config/text-setup.typ": *
#import "config/styling.typ": *

// ============================================================================
// INITIALIZE SETUP
// ============================================================================

#setup-page()
#setup-text()
#setup-styling()

// ============================================================================
// FRONT MATTER
// ============================================================================

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

// ============================================================================
// MAIN CONTENT - Start Arabic numerals
// ============================================================================

#set page(numbering: "1")
#counter(page).update(1)
#set heading(numbering: "1.1.1.1")

#include "chapters/chapter1-introduction.typ"
#include "chapters/chapter2-literature.typ"
#include "chapters/chapter3-methodology.typ"
#include "chapters/chapter4-results.typ"
#include "chapters/chapter5-conclusion.typ"

// ============================================================================
// BACK MATTER
// ============================================================================

#include "backmatter/references.typ"
#include "backmatter/appendices.typ"
// ============================================================================
// THESIS VARIABLES CONFIGURATION
// ============================================================================
// Edit this file to update all thesis information automatically
// All variables are used throughout the thesis document
// ============================================================================

// ============================================================================
// STUDENT INFORMATION
// ============================================================================

#let student = (
  // Basic Information
  full-name-vi: "Nguyễn Thanh Phát",
  full-name-en: "Nguyen Thanh Phat",
  student-id: "B2005853",
  class: "DI20V7F1",
  cohort: "46",
  cohort-year: "2019-2023",

  // Contact Information (optional)
  email: "b1910402@student.ctu.edu.vn",
  phone: "0123456789",

  // Academic Information
  major-vi: "Công nghệ Thông tin",
  major-en: "BACHELOR OF ENGINEERING IN \n INFORMATION TECHNOLOGY",
  program-vi: "Chất lượng cao",
  program-en: "High-Quality Program",

  // Personal (optional, for declaration page)
  dob: "17/10/2002",
  place-of-birth-vi: "Vĩnh Long",
  place-of-birth-en: "Vinh Long",
  hometown-vi: "Phường An Hòa, Quận Ninh Kiều, TP. Cần Thơ",
  hometown-en: "An Hoa Ward, Ninh Kieu District, Can Tho City"
)

// ============================================================================
// ADVISOR INFORMATION
// ============================================================================

#let advisor = (
  // Primary Advisor
  primary: (
    title_vi: "TS.",
    title-en: "Dr.",
    full-name-vi: "Trần Công Án",
    full-name-en: "Tran Cong An",
    position-vi: "Giảng viên",
    position-en: "Lecturer",
    department-vi: "Khoa Công nghệ Thông tin và Truyền thông",
    department-en: "College of Information and Communication Technology",
  ),
  // Co-Advisor (if any, leave empty if none)
  secondary: (
    title_vi: "",
    title-en: "",
    full-name-vi: "",
    full-name-en: "",
    position-vi: "",
    position-en: "",
  ),
)

// ============================================================================
// THESIS INFORMATION
// ============================================================================

#let thesis = (
  // Thesis Title
  title_vi: "Phát triển ứng dụng thương mại điện tử bán thời trang tích hợp gợi ý và tìm kiếm sản phẩm bằng hình ảnh",
  title-en: "Building a Fashion E-commerce Application with Recommendation and Image-based Product Search",
  
  // Short Title (for headers)
  short-title_vi: "ReSys: Hệ TMĐT Thời trang",
  short-title-en: "ReSys: Fashion E-commerce System",
  
  // Thesis Type
  type-vi: "LUẬN VĂN TỐT NGHIỆP",
  type-en: "GRADUATION THESIS",
  degree-vi: "ĐẠI HỌC",
  degree-en: "BACHELOR OF ENGINEERING IN",
  
  // Submission Information
  submission_day: "20",
  submission_month: "12",
  submission_year: "2025",

  submission_month_text_en: "December",
  submission_month_text_vi: "Tháng 12",

  submission_day_text_en: "20th",
  submission_day_text_vi: "20",

  submission_location_en: "Can Tho",
  submission_location_vi: "Cần Thơ",

  // Defense Information
  defense_date: "24/12/2025",
  defense_time: "13:00",
  defense_room: "E4.102",
  
  // Academic Year
  academic-year: "2024-2025",
  semester: "Học kỳ 1",
)

// ============================================================================
// UNIVERSITY INFORMATION
// ============================================================================

#let university = (
  // University Names
  name-vi: "ĐẠI HỌC CẦN THƠ",
  name-en: "CAN THO UNIVERSITY",
  short-vi: "ĐHCT",
  short-en: "CTU",

  // College/Faculty
  college-vi: "TRƯỜNG ĐẠI HỌC CÔNG NGHỆ THÔNG TIN VÀ TRUYỀN THÔNG",
  college-en: "COLLEGE OF INFORMATION AND COMMUNICATION TECHNOLOGY",
  college-short-vi: "Trường CNTT&TT",
  college-short-en: "College of ICT",

  // Department
  department-vi: "KHOA CÔNG NGHỆ THÔNG TIN",
  department-en: "DEPARTMENT OF INFORMATION TECHNOLOGY",

  // Ministry
  ministry-vi: "BỘ GIÁO DỤC VÀ ĐÀO TẠO",
  ministry-en: "MINISTRY OF EDUCATION AND TRAINING",

  // Contact Information
  address-vi: "Khu II, đường 3/2, P. Xuân Khánh, Q. Ninh Kiều, TP. Cần Thơ",
  address-en: "Khu II, 3/2 Street, Xuan Khanh Ward, Ninh Kieu District, Can Tho City",
  website: "https://cit.ctu.edu.vn",
  phone: "(0292) 3832663",
)

// ============================================================================
// DOCUMENT SETTINGS
// ============================================================================

#let doc_settings = (
  // Language Settings
  primary-language: "en", // "vi" or "en"
  secondary-language: "vi",

  // Page Settings
  paper-size: "a4",
  margin-left: 3cm,
  margin-right: 2cm,
  margin-top: 2cm,
  margin-bottom: 2cm,
  
  // Text Settings
  font-family: "Times New Roman",
  font-size: 13pt,
  line-spacing: 1.5, // 1.5 for undergraduate, 1.2 for graduate
  paragraph-spacing-before: 3pt,
  paragraph-spacing-after: 3pt,
  first-line-indent: 1cm,
  
  // Heading Sizes
  chapter-size: 14pt,
  section-size: 13pt,
  subsection-size: 13pt,
  
  // Figure/Table Settings
  caption-size: 13pt,
  table-content-size: 12pt,
  
  // Border Settings (for cover pages)
  border-color: rgb(0, 51, 153), // CTU Blue
  border-thickness: 2pt,
  border-inset: 1.5cm,
)

// ============================================================================
// ABSTRACT KEYWORDS
// ============================================================================

#let keywords = (
  vietnamese: (
    "thương mại điện tử",
    "thời trang",
    "hệ gợi ý",
    "tìm kiếm bằng hình ảnh",
    "học sâu",
    "thị giác máy tính",
  ),
  english: (
    "e-commerce",
    "fashion",
    "recommendation system",
    "image-based search",
    "deep learning",
    "computer vision",
  ),
)


// ============================================================================
// COMMITTEE INFORMATION (for approval page)
// ============================================================================

#let committee = (
  chairman: (
    title: "TS.",
    name: "Trần Công Án",
    position: "Chủ tịch Hội đồng",
  ),
  secretary: (
    title: "TS.",
    name: "Trần Công Án",
    position: "Thư ký",
  ),
  reviewer1: (
    title: "TS.",
    name: "Phạm Thế Phi",
    position: "Phản biện 1",
  ),
  reviewer2: (
    title: "TS.",
    name: "Thái Minh Tuấn",
    position: "Phản biện 2",
  ),
)
#let get_advisor_full_name(lang: "en") = {
  if lang == "vi" {
    advisor.primary.title_vi + " " + advisor.primary.full-name-vi
  } else {
    advisor.primary.title-en + " " + advisor.primary.full-name-en
  }
}

// ============================================================================
// ABBREVIATIONS
// ============================================================================

#let abbreviations = (
  ("API", "Application Programming Interface", "Giao diện lập trình ứng dụng"),
  ("CSDL", "Cơ sở dữ liệu", "Database"),
  ("CDM", "Conceptual Data Model", "Mô hình dữ liệu mức quan niệm"),
  ("CNTT", "Công nghệ Thông tin", "Information Technology"),
  ("CTU", "Can Tho University", "Đại học Cần Thơ"),
  ("ĐHCT", "Đại học Cần Thơ", "Can Tho University"),
  ("HTML", "HyperText Markup Language", ""),
  ("HTTP", "HyperText Transfer Protocol", ""),
  ("JSON", "JavaScript Object Notation", ""),
  ("MVC", "Model-View-Controller", ""),
  ("NoSQL", "Not Only SQL", "Cơ sở dữ liệu không quan hệ"),
  ("REST", "Representational State Transfer", ""),
  ("SQL", "Structured Query Language", ""),
  ("UI/UX", "User Interface/User Experience", "Giao diện/Trải nghiệm người dùng"),
)

// ============================================================================
// HELPER FUNCTIONS
// ============================================================================

// Get full name with title
#let get_advisor_full_name(lang: "vi") = {
  if lang == "vi" {
    advisor.primary.title_vi + " " + advisor.primary.full-name-vi
  } else {
    advisor.primary.title-en + " " + advisor.primary.full-name-en
  }
}

// Get thesis title
#let get-thesis-title(lang: "vi") = {
  if lang == "vi" {
    thesis.title_vi
  } else {
    thesis.title-en
  }
}

// Get submission date
#let get_submission_date(lang) = {
  if lang == "vi" {
    thesis.submission_location_vi + ", " + thesis.submission_month + "/" + thesis.submission_year
  } else {
    thesis.submission_location_en + ", " + thesis.submission_month + "/" + thesis.submission_year
  }
}


#let get_submission_date-text(lang: "en") = {
  if lang == "vi" {
    thesis.submission_location_vi + ", "
    + thesis.submission_day + "/"
    + thesis.submission_month_text_vi + "/"
    + thesis.submission_year
  } else {
    thesis.submission_location_en + ", "
    + thesis.submission_month_text_en + " "
    + thesis.submission_day + ", "
    + thesis.submission_year
  }
}


// Format keywords
#let format-keywords(lang: "vi") = {
  let kw = if lang == "vi" { keywords.vietnamese } else { keywords.english }
  kw.join(", ")
}

// ============================================================================
// EXPORT ALL VARIABLES
// ============================================================================

// Make all variables available when importing this file
#let all-variables = (
  student: student,
  advisor: advisor,
  thesis: thesis,
  university: university,
  doc_settings: doc_settings,
  keywords: keywords,
  committee: committee,
  abbreviations: abbreviations,
)

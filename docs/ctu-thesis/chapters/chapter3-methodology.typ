// Chapter 3: Methodology
= METHODOLOGY

This chapter details the research methodology employed in the design and development of the web-based student management system. An agile development approach was adopted to ensure flexibility, iterative progress, and continuous feedback integration throughout the project lifecycle.

== Research Approach

The project followed a design science research methodology, focusing on creating an innovative artifact (the student management system) to solve a real-world problem. This approach involves identifying a problem, defining objectives, designing and developing an artifact, demonstrating its utility, and evaluating its performance.

== Agile Development Model

An agile software development model, specifically Scrum, was chosen for its iterative and incremental nature. This allowed for adaptability to changing requirements and facilitated close collaboration with potential users. The project was divided into several sprints, each focusing on delivering a functional increment of the system.

=== Phases of Agile Development

==== 1. Requirements Analysis

This phase involved gathering and analyzing the functional and non-functional requirements for the student management system.

===== Techniques Used

+ *Interviews:* Structured interviews were conducted with key stakeholders, including university administrators, faculty members, and a sample of students, to understand their needs, pain points, and expectations from the new system.
+ *Surveys:* Online surveys were distributed to a wider student population to gather quantitative data on desired features and priorities.
+ *Document Analysis:* Existing documents, such as academic regulations, student handbooks, and current administrative workflows, were reviewed to identify critical data points and processes.

===== Key Requirements

+ User authentication and authorization (students, faculty, administrators)
+ Student profile management (personal information, academic history)
+ Course catalog browsing and enrollment
+ Grade entry and viewing
+ Class scheduling and attendance tracking
+ Automated reporting (transcripts, class lists)
+ Responsive user interface
+ Robust data security and privacy

==== 2. System Design

Based on the gathered requirements, the system architecture and detailed design were formulated.

===== Architectural Design

The system adopted a three-tier architecture:
+ *Presentation Layer (Frontend):* Developed using React.js, responsible for the user interface and user interaction.
+ *Application Layer (Backend):* Developed using Node.js with Express.js, handling business logic, API endpoints, and data processing.
+ *Data Layer (Database):* MongoDB was chosen for data persistence due to its flexibility and scalability, storing student records, course information, and other system data.

===== Database Design

A NoSQL document-oriented approach was used for MongoDB. Collections were designed to store related data, such as `students`, `courses`, `enrollments`, and `grades`. Relationships between collections were managed through references, optimizing for read performance and schema flexibility.

===== UML Diagrams

Unified Modeling Language (UML) diagrams were employed to visualize the system's structure and behavior.
+ *Use Case Diagrams:* Illustrated the interactions between users (actors) and the system.
+ *Class Diagrams:* Depicted the static structure of the system, showing classes, attributes, methods, and relationships.
+ *Sequence Diagrams:* Showed the interactions between objects in a sequential order, particularly for key processes like student enrollment or grade submission.
+ *Activity Diagrams:* Modeled the workflow of various processes within the system.

==== 3. Implementation

The system was developed iteratively in accordance with the agile methodology.

===== Frontend Development

React.js was used to build a responsive and intuitive user interface. Key components included:
+ User dashboards (student, faculty, admin)
+ Forms for data entry (e.g., course registration, grade submission)
+ Data display components (e.g., student transcripts, course schedules)
+ Navigation and routing using React Router.

===== Backend Development

Node.js with Express.js was utilized to create RESTful APIs. Key functionalities implemented included:
+ User authentication and authorization using JWT (JSON Web Tokens).
+ API endpoints for CRUD (Create, Read, Update, Delete) operations on student, course, and grade data.
+ Business logic for enrollment rules, grade calculations, and report generation.
+ Integration with MongoDB using Mongoose ODM (Object Data Modeling).

==== 4. Testing

A multi-faceted testing strategy was employed to ensure the system's quality, functionality, and performance.

===== Unit Testing

Individual functions and components were tested in isolation to verify their correctness. Jest was used for React.js components, and Mocha/Chai for Node.js backend functions.

===== Integration Testing

This involved testing the interactions between different modules and services, particularly between the frontend and backend, and the backend with the database. Automated integration tests ensured that data flowed correctly across the system.

===== User Acceptance Testing (UAT)

A selected group of end-users (students, faculty, administrators) participated in UAT. They tested the system against predefined scenarios to confirm that it met their requirements and was user-friendly. Feedback from UAT was incorporated into subsequent development sprints.

===== Performance Testing

Load testing was conducted to assess the system's responsiveness and stability under various user loads, ensuring it could handle the anticipated number of concurrent users during peak times (e.g., course registration periods).

==== 5. Deployment

The system was deployed to a cloud platform (e.g., Heroku, AWS EC2) to ensure accessibility and scalability. A continuous integration/continuous deployment (CI/CD) pipeline was established to automate the build, test, and deployment processes, facilitating rapid iterations and updates.

== Tools and Technologies

#table(
  columns: (auto, auto, 1fr), // Adjust column widths as needed
  align: (left, left, left), // Default alignment for content
  // Table header
  [Category], [Tool/Technology], [Description],
  // Table rows
  [Frontend], [React.js], [JavaScript library for UI development],
  [], [HTML5, CSS3], [Standard web languages],
  [], [Axios], [HTTP client for API requests],
  [Backend], [Node.js], [JavaScript runtime environment],
  [], [Express.js], [Web framework for Node.js],
  [], [Mongoose], [ODM for MongoDB],
  [], [JWT], [JSON Web Tokens for authentication],
  [Database], [MongoDB], [NoSQL document database],
  [Testing], [Jest], [JavaScript testing framework (frontend)],
  [], [Mocha, Chai], [JavaScript testing frameworks (backend)],
  [Version Control], [Git, GitHub], [Distributed version control system],
  [Deployment], [Heroku/AWS EC2], [Cloud platform for hosting],
  [UI/UX], [Figma], [Prototyping and design tool],
)

== Conclusion

The agile development methodology, combined with a robust technology stack, provided a structured yet flexible framework for developing the student management system. The iterative nature of the approach allowed for continuous improvement and ensured that the final product effectively addressed the needs of Can Tho University. The subsequent chapters will present the detailed implementation and evaluation of the system.
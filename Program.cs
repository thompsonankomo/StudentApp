﻿
using StudentManagementSystem;
using System.Diagnostics.Contracts;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

public delegate void Alert(params dynamic[] massage); //delegate for showing message on screen when any process successfully completed
public class MainProgram
{
    public static void Main(string[] args)
    {
        WelcomeScreen();

        //Read data from json and convert them to list of objects.
        FileHandler fileHandler = new FileHandler();
        List<dynamic> studentList = fileHandler.ReadFiles("students.json");
        List<dynamic> courseList = fileHandler.ReadFiles("courses.json");

        DelegateAndEventsUtils Event = new DelegateAndEventsUtils();
        Event.processFinished += EventHandler;

        while (true)
        {
    
            
            Startingpoint:
            ShowInfo(studentList, "student"); //show all the existing students on the screen.
            int input = InputMenuScreen();
            //Add new student
            if (input == 1)
            {
                FailedLabel:
                Student student = InputStudentScreen();
                //Validating whether  inputted student id is unique or not
                foreach(var std in studentList)
                {
                    if (std.StudentID == student.StudentID)
                    {
                        Event.ShowFailedMessage("Sorry. Your given student id-", student.StudentID, "is already taken by another student. Please try again.\n");
                        goto FailedLabel;
                    }
                }
                studentList.Add(student);
                Event.ShowCompletionMessage("Student", student.FirstName, student.MiddleName, student.LastName, "succuessfully added to the list.");

            }
            //view student details option
            else if(input == 2)
            {

                Console.Write("\nEnter Student id: ");
                string id = Console.ReadLine();
                Console.ForegroundColor = ConsoleColor.DarkCyan;

                int index = GetIndividualStudentIndex(studentList, id);
                if (index == -1) Console.WriteLine("Student not found.\n");
                else
                {
                    studentList[index].ShowFormattedOutput();
                    int option = InputSemesterScreen();
                    
                    if(option == 1)
                    {
                        SemesterStart:
                        Semester newSemester = InputSemesterScreenDetails();
                        bool flag = true;

                        //checking whether the inputted current semester already taken by the current student or not 
                        if (studentList[index].AttendedSemester.Count > 0)
                        {
                            foreach (var semester in studentList[index].AttendedSemester)
                            {
                                if (semester.SemesterCode.ToLower() == newSemester.SemesterCode.ToLower() && semester.Year.ToLower() == newSemester.Year.ToLower())
                                {
                                    flag = false;
                                    Console.WriteLine("You already enrolled in this semester. Please try another one..");
                                    goto SemesterStart;
                                }
                            }
                        }
                        //If the inputted semester is not taken by the current student
                        if(flag)
                        {
                            studentList[index].AttendedSemester.Add(newSemester);
                            List<dynamic> updatedCourseList = courseList;
                            List<dynamic> existingCourseList = new List<dynamic>();
                            
                            //Filtering those courses from the courseList which are already completed by the current student
                            if(studentList[index].CoursesInEachSemester.Count > 0)
                            {
                                for(int i=0; i < studentList[index].CoursesInEachSemester.Count; i++)
                                {
                                    foreach (var course in studentList[index].CoursesInEachSemester[i])
                                    {
                                        foreach(var c in updatedCourseList)
                                        {
                                            if(c.CourseID == course.CourseID)
                                            {
                                                existingCourseList.Add(course.CourseID);
                                            }
                                        }
                                    }
                                }

                                updatedCourseList = updatedCourseList.Where(x => !existingCourseList.Contains(x.CourseID)).ToList();
                                
                            }


                            ShowInfo(updatedCourseList, "course"); //Show all the courses on the screen that are available for the current student

                            //If all the courses completed by the current student(i.e updateCourseList.count == 0), then there is no meaning to show the input course menu
                            if(updatedCourseList.Count > 0)
                            {
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine("Note: After selecting your courses, you cannot change them or add new ones to this semester.");
                              CountLabel:
                                Console.Write("Enter how many courses you want to enroll for this ({0} {1}) semester: ", newSemester.SemesterCode, newSemester.Year);

                                bool isValid = int.TryParse(Console.ReadLine(), out int n);
                                if (!isValid)
                                {
                                    Console.WriteLine("Enter integer value..");
                                    goto CountLabel;
                                }
                                Console.WriteLine();

                                //choosing courses from the available courses for the current student
                                List<Course> chosenCourses = new List<Course>();
                                for (int i = 0; i < n; i++)
                                {
                                    Console.Write("Enter Course ID: ");
                                    string courseID = Console.ReadLine();

                                    bool isFound = false;
                                    foreach (var course in updatedCourseList)
                                    {
                                        if (course.CourseID == courseID)
                                        {
                                            isFound = true;
                                            chosenCourses.Add(course);
                                        }
                                    }

                                    if (!isFound)
                                    {
                                        Console.WriteLine("Course ID is not valid. Please enter a valid Course ID.");
                                        i--;
                                    }
                                }
                                studentList[index].CoursesInEachSemester.Add(chosenCourses);
                                Event.ShowCompletionMessage("You have successfully enrolled in", chosenCourses.Count, "courses for", newSemester.SemesterCode, newSemester.Year,"semester.");

                            }
                        }


                    }
                    else if(option == 2)
                    {
                        goto Startingpoint;
                    }
                    else
                    {
                        break;
                    }
                    
                }
            }

            //Delete specific student
            else if(input == 3)
            {
              Delete:
                Console.Write("Enter Student id: ");
                string id = Console.ReadLine();

                int index = GetIndividualStudentIndex(studentList, id);
                if (index == -1)
                {
                    Console.WriteLine("Please provide a valid and existing student ID.");
                    goto Delete;
                }
                else
                {
                    studentList.RemoveAt(index);
                    Event.ShowCompletionMessage("Successfully deleted student id-", id, "from the list");
                }
            }
            else
            {
                break;
            }

        }
        fileHandler.WriteFiles("students.json", studentList);
        Event.ShowExitMessage("Thank you for using our service.");

        Console.ReadKey();
    }

    //Input Menu for choosing different operations
    private static int InputMenuScreen()
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("Choose your option: ");
        Console.WriteLine("Type 1 for: Add new student");
        Console.WriteLine("Type 2 for: View student details");
        Console.WriteLine("Type 3 for: Delete specific student");
        Console.WriteLine("Press any key to exit");
        var choice = Console.ReadLine();
        int option;
        try
        {
            option = int.Parse(choice);
        }
        catch
        {
            option = 4; // any charachter or null value will be populated with option 4
        }
        return option;
    }

    //Input screen for adding new student
    private static Student InputStudentScreen()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Student student = new Student();
        FName:
        Console.Write("Enter student first name: ");
        var fName = Console.ReadLine();
        student.FirstName = fName;
        if(student.FirstName == "None")
        {
            goto FName;
        }
       MName:
        Console.Write("Enter student middle name: ");
        var mName = Console.ReadLine();
        student.MiddleName = mName;
        if (student.MiddleName == "None")
        {
            goto MName;
        }
       LName:
        Console.Write("Enter student last name: ");
        var lName = Console.ReadLine();
        student.LastName = lName;
        if (student.LastName == "None")
        {
            goto LName;
        }
       ID:
        Console.Write("Enter Student ID in the format XXX-XXX-XXX: ");
        var id = Console.ReadLine();
        student.StudentID = id;
        if (student.StudentID == "None")
        {
            goto ID;
        }

        student.JoiningBatch = JoiningSemester();

    Dept:
        Console.Write("Enter your Department (Type 1 for ComputerScience, 2 for BBA, 3 for English): ");
        var dept = Console.ReadLine();
        bool isInteger = int.TryParse(dept, out int value);
        
        if(isInteger)
        {
            if(value >= 1 && value <=3)
            {
                student.Department = value;
            }
            else
            {
                Console.WriteLine("Department value should be  in range [1-3].");
                goto Dept;
            }
        }
        if(!isInteger)
        {
            Console.WriteLine("Enter integer value from 1-3 for choosing your department.");
            goto Dept;
        }
    DegRee:
        Console.Write("Enter your Degree (Type 1 for BSC, 2 for BBA, 3 for BA, 4 for MSC, 5 for MBA, 6 for MA): ");
        var deg = Console.ReadLine();
        bool isNumber = int.TryParse(deg, out int val);
        if (isNumber)
        {
            if (val >= 1 && val <= 6)
            {
                student.Degree = val;
            }
            else
            {
                Console.WriteLine("Degree value should be  in range [1-6].");
                goto DegRee;
            }
        }
        if (!isNumber)
        {
            Console.WriteLine("Enter integer value from 1-6 for choosing your degree.");
            goto DegRee;
        }
        

        return student;
    }

    //Based on the current date time, Student Joining Batch will be populated by this function
    private static Semester JoiningSemester()
    {

        DateTime date = DateTime.Now;
        int month = date.Month;
        string year = Convert.ToString(date.Year);
        string semesterCode;

        if (month <= 4)
        {
            semesterCode = "Summer";
        }
        else if (month <= 8)
        {
            semesterCode = "Fall";
        }
        else
        {
            semesterCode = "Spring";
        }

        return new Semester(semesterCode, year); ;
    }

    //This function return specific student data index in the studentList by taking student id
    private static int GetIndividualStudentIndex(List<dynamic> studentList, string id)
    {
        int index = -1, i = 0;
        foreach (var student in studentList)
        {
            if (student.StudentID == id)
            {
                index = i;
            }
            i++;
        }

        return index;
    }
    
    private static int InputSemesterScreen()
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("Type 1 for : Add semester.");
        Console.WriteLine("Type 2 for : Return to Main menu.");
        Console.WriteLine("Type Any other key to exit the application.\n");

        string choice = Console.ReadLine();
        int option;

        try
        {
            option = Convert.ToInt32(choice);
        }
        catch
        {
            option = 3;
        }

        return option;
    
    }

    private static Semester InputSemesterScreenDetails()
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Semester semester = new Semester();

        SemCode:
        Console.Write("Enter a Semester name from ('Summer,' 'Fall,' 'Spring'): ");
        var code = Console.ReadLine();
        semester.SemesterCode = code;
        if(semester.SemesterCode == "None")
        {
            goto SemCode;
        }
      SemYear:
        Console.Write("Enter Year in the format YYYY. e.g. 2022: ");
        var year = Console.ReadLine();
        semester.Year = year;
        if (semester.Year == "None")
        {
            goto SemYear;
        }

        return semester;

    }

    //This method shows data on the screen by taking generic list
    private static void ShowInfo(List<dynamic> lists, string type)
    {
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine("\n---------------------------------------------------");
        Console.WriteLine($"|           Existing {type}s are:                | ");
        Console.WriteLine("---------------------------------------------------");
        Console.WriteLine($"{type.ToUpper()} ID  - {type.ToUpper()} NAME");

        if (lists.Count > 0)
        {
            foreach (var list in lists)
            {
                if (type == "student")
                {
                    
                    Console.WriteLine($"{list.StudentID} - {list.FirstName}");
                }

                else
                {
                    Console.WriteLine($"{list.CourseID} - {list.CourseName}");
                }
            }
        } 
        else
        {
            Console.WriteLine($"No {type} exits till now");
        }

        Console.WriteLine();
    }

    private static void EventHandler(params dynamic[] massages)
    {
        Console.WriteLine();
        foreach (var massage in massages)
        {
            Console.Write($"{massage} ");
        }
        Console.WriteLine();
    }

    private static void WelcomeScreen()
    {
        Console.Title = "Student Management System";
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\t\t\t************************************************************");
        Console.WriteLine("\t\t\t            Welcome to Student Management App\n                 ");
        Console.WriteLine("\t\t\t************************************************************");

    }
    
}
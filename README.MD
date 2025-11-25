# Weather Data Project - Analysis Tool

This project is a console application written in C# that reads, stores, and analyzes temperature and humidity data from a CSV file. The application utilizes **Entity Framework Core** with the **Code First** approach for database management.

## 🚀 Getting Started

To run this program, you need Visual Studio with .NET installed.

### Prerequisites
* .NET SDK (9.0)
* SQL Server LocalDB (usually included with Visual Studio)

### Installation and Usage
1.  Open the file `VaderDataProjekt.sln` in Visual Studio.
2.  Ensure that the file `TempFuktData.csv` is located in the **UI** folder.
3.  Set the **UI** project as the "Startup Project".
4.  Press **Start** (or F5).

The application will automatically:
1.  Create the database (`WeatherDB`) if it does not already exist.
2.  Import and clean the data from the CSV file (handling errors and duplicates).
3.  Execute all analyses and present the results in the console.

---

## 🏗 Architecture

The solution is divided into three layers (Projects) to adhere to the Separation of Concerns principle:

* **Core:** Contains business logic and models (`WeatherData`, `WeatherProcessor`). All calculations are performed here.
* **DataAccess:** Handles the database connection (`WeatherContext`) and import logic (`CsvImporter`).
* **UI:** Console interface that presents the data to the user.

---

## 📊 Features

The application fulfills all requirements for Pass (G) and Pass with Distinction (VG).

### Basic Analysis (G)
* **Temperature:** Calculates averages and sorts days from hottest to coldest.
* **Humidity:** Sorts days from driest to most humid.
* **Mold Risk:** Calculates a risk index based on temperature and humidity. Sorts days from least to greatest risk.
* **Meteorological Seasons:** Identifies dates for Meteorological Autumn (5 days < 10°C) and Winter (5 days ≤ 0°C).

### Advanced Analysis (VG)
* **Balcony Door:** Detects when the balcony door has been open by analyzing patterns where indoor temperature drops while outdoor temperature rises. Calculates total duration per day.
* **Temperature Difference:** Calculates and sorts days based on the difference between indoor and outdoor temperatures.

---

## 🛠 Tech Stack

* **Language:** C#
* **ORM:** Entity Framework Core (SQL Server)
* **CSV Handling:** CsvHelper (with custom error handling for dirty data)
* **LINQ:** Used for all filtering, grouping, and data calculation.
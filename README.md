# **Azure DevOps Viewer**

## **Overview**
Azure DevOps Viewer is a Windows Forms application that connects to an Azure DevOps organization and retrieves project, iteration, and work item details. This application fetches **Epics** from a selected iteration and displays them in a **DataGridView**.

## **Features**
- Connects to **Azure DevOps** using a **Personal Access Token (PAT)** stored as an **environment variable**.
- Retrieves and displays **Azure DevOps projects**.
- Lists **iterations** for a selected project.
- Fetches **Epics** within a chosen iteration.
- Displays **Epics** along with **Effort** and **Story Points**.
- Computes and shows the difference between **Effort and Story Points**.

## **Prerequisites**
- **.NET Framework / .NET Core** (depending on your setup)
- **Visual Studio**
- **RestSharp** and **Newtonsoft.Json** libraries (installed via NuGet)
- An **Azure DevOps PAT** with read access to work items and projects.

## **Setting Up the Environment Variable**
To securely store the **Personal Access Token (PAT)**, set it as an **environment variable** named `AZURE_DEVOPS_PAT`.

### **Windows (Command Prompt)**
```cmd
setx AZURE_DEVOPS_PAT "your_personal_access_token_here"
```

### **Windows (PowerShell)**
```powershell
[System.Environment]::SetEnvironmentVariable("AZURE_DEVOPS_PAT", "your_personal_access_token_here", [System.EnvironmentVariableTarget]::User)
```

### **Linux/macOS (Terminal)**
```bash
export AZURE_DEVOPS_PAT="your_personal_access_token_here"
```
To make this persistent, add the line to `~/.bashrc` or `~/.zshrc`.

## **Installation**
1. **Clone or download the project.**
2. **Open the project in Visual Studio.**
3. **Install the required NuGet packages:**
   - `Newtonsoft.Json`
   - `RestSharp`
4. **Ensure the environment variable `AZURE_DEVOPS_PAT` is set.**
5. **Restart Visual Studio** to apply the environment variable.

## **Code Breakdown**
### **Connecting to Azure DevOps**
The `personalAccessToken` is retrieved from the environment variable:
```csharp
private string personalAccessToken = Environment.GetEnvironmentVariable("AZURE_DEVOPS_PAT");
```
If the **PAT** is missing, an error message is displayed:
```csharp
if (string.IsNullOrEmpty(personalAccessToken))
{
    MessageBox.Show("Error: Personal Access Token (PAT) is missing. Please set the environment variable AZURE_DEVOPS_PAT.",
                    "Authentication Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
    return;
}
```

### **Fetching Projects**
Uses **RestSharp** to call **Azure DevOps API**:
```csharp
var client = new RestClient($"https://dev.azure.com/{organization}/_apis/projects?api-version=6.0");
var request = new RestRequest { Method = Method.Get };
request.AddHeader("Authorization", $"Basic {Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($":{personalAccessToken}"))}");
RestResponse response = client.Execute(request);
```

### **Fetching Iterations**
```csharp
var client = new RestClient($"https://dev.azure.com/{organization}/{project}/_apis/work/teamsettings/iterations?api-version=7.1");
```

### **Fetching Work Items (Epics)**
```csharp
var wiql = new
{
    query = $@"
    Select [System.Id]
    From WorkItems
    Where [System.IterationPath] = '{iteration}' 
      AND [System.WorkItemType] = 'Epic'"
};
request.AddJsonBody(wiql);
```

### **Fetching Epic Details**
Retrieves **Epics** along with their **Effort**, **State**, and **Story Points**:
```csharp
var client = new RestClient($"https://dev.azure.com/{organization}/{project}/_apis/wit/workitems/{epicId}?$expand=relations&api-version=6.0");
```

## **Running the Application**
1. **Ensure the environment variable `AZURE_DEVOPS_PAT` is set.**
2. **Restart Visual Studio** to apply the changes.
3. **Run the application.**
4. **Select a project and iteration to view Epics.**

## **Troubleshooting**
### **1. "Error: Personal Access Token (PAT) is missing."**
- Ensure the **environment variable** is set correctly.
- Restart **Visual Studio** and try again.

### **2. "Error fetching projects/iterations/work items."**
- Check if the **PAT** has the necessary permissions.
- Ensure **Azure DevOps API version** is correct.

### **3. "No Epics found in this iteration."**
- Ensure there are **Epics** in the selected iteration.

## **Future Enhancements**
- Add filtering by **work item type**.
- Implement **UI improvements** for a better user experience.
- Enable **bulk operations** for managing work items.

## **License**
This project is **open-source** and free to use.


using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace AzureDevOpsViewer
{
    public partial class Form1 : Form
    {
        private string organization = "access-devops"; // Replace with your organization name
        private string personalAccessToken = Environment.GetEnvironmentVariable("AZURE_DEVOPS_PAT");


        //
        public Form1()
        {
            InitializeComponent();
            if (string.IsNullOrEmpty(personalAccessToken))
            {
                MessageBox.Show("Error: Personal Access Token (PAT) is missing. Please set the environment variable AZURE_DEVOPS_PAT.",
                                "Authentication Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            FetchProjects();

            // Wire up the event for product selection
            cmbProducts.SelectedIndexChanged += cmbProducts_SelectedIndexChanged;

            dataGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells; // Adjust column size
            dataGridView.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;     // Adjust row size
        }

        private void FetchProjects()
        {
            var client = new RestClient($"https://dev.azure.com/{organization}/_apis/projects?api-version=6.0");

            var request = new RestRequest
            {
                Method = Method.Get
            };
            request.AddHeader("Authorization", $"Basic {Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($":{personalAccessToken}"))}");

            RestResponse response = client.Execute(request);

            if (response.IsSuccessful)
            {
                dynamic data = JsonConvert.DeserializeObject(response.Content);
                cmbProducts.Items.Clear();

                List<string> projects = new List<string>();
                foreach (var project in data.value)
                {
                    projects.Add((string)project.name);
                }

                projects = projects.OrderBy(project => project).ToList();
                cmbProducts.Items.AddRange(projects.ToArray());

                if (cmbProducts.Items.Count > 0)
                {
                    cmbProducts.SelectedIndex = 0;
                }
            }
            else
            {
                MessageBox.Show("Error fetching projects: " + response.Content, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void cmbProducts_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbProducts.SelectedItem != null)
            {
                string selectedProject = cmbProducts.SelectedItem.ToString();
                FetchIterations(selectedProject);
            }
        }

        private void FetchIterations(string project)
        {
           var client = new RestClient($"https://dev.azure.com/{organization}/{project}/_apis/work/teamsettings/iterations?api-version=7.1");
       


            var request = new RestRequest
            {
                Method = Method.Get
            };
            request.AddHeader("Authorization", $"Basic {Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($":{personalAccessToken}"))}");

            RestResponse response = client.Execute(request);

            if (response.IsSuccessful)
            {
                dynamic data = JsonConvert.DeserializeObject(response.Content);
                cmbIterations.Items.Clear();

                HashSet<string> parentIterations = new HashSet<string>();
                foreach (var iteration in data.value)
                {
                    string iterationPath = (string)iteration.path;

                    if (iterationPath.Contains("\\"))
                    {
                        string parentIteration = string.Join("\\", iterationPath.Split('\\').Take(2));
                        if (parentIterations.Add(parentIteration))
                        {
                            cmbIterations.Items.Add(parentIteration);
                        }
                    }
                    else
                    {
                        if (parentIterations.Add(iterationPath))
                        {
                            cmbIterations.Items.Add(iterationPath);
                        }
                    }
                }

                if (cmbIterations.Items.Count > 0)
                {
                    cmbIterations.SelectedIndex = 0;
                }
            }
            else
            {
                MessageBox.Show("Error fetching iterations: " + response.Content, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnFetchData_Click(object sender, EventArgs e)
        {
            if (cmbProducts.SelectedItem == null || cmbIterations.SelectedItem == null)
            {
                MessageBox.Show("Please select a product and iteration.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string project = cmbProducts.SelectedItem.ToString();
            string iteration = cmbIterations.SelectedItem.ToString();

            FetchWorkItems(project, iteration);
        }

        private void FetchWorkItems(string project, string iteration)
        {
            var client = new RestClient($"https://dev.azure.com/{organization}/{project}/_apis/wit/wiql?api-version=6.0");

            var request = new RestRequest
            {
                Method = Method.Post
            };
            request.AddHeader("Authorization", $"Basic {Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($":{personalAccessToken}"))}");

            var wiql = new
            {
                query = $@"
                Select [System.Id]
                From WorkItems
                Where [System.IterationPath] = '{iteration}' 
                  AND [System.WorkItemType] = 'Epic'"
            };
            request.AddJsonBody(wiql);

            RestResponse response = client.Execute(request);

            if (response.IsSuccessful)
            {
                dynamic data = JsonConvert.DeserializeObject(response.Content);
                dataGridView.Rows.Clear();

                if (dataGridView.Columns.Count == 0)
                {
                    dataGridView.Columns.Add("EpicID", "Epic ID");
                    dataGridView.Columns.Add("EpicState", "Epic State");
                    dataGridView.Columns.Add("Title", "Title");
                    dataGridView.Columns.Add("Effort", "Effort");
                    dataGridView.Columns.Add("StoryPoints", "Story Points");
                    dataGridView.Columns.Add("Difference", "Effort - Story Points");
                }

                if (data.workItems != null && data.workItems.Count > 0)
                {
                    foreach (var item in data.workItems)
                    {
                        string epicId = item.id.ToString();
                        FetchEpicDetails(project, epicId);
                    }
                }
                else
                {
                    MessageBox.Show("No Epics found in this iteration.", "No Epics", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            else
            {
                MessageBox.Show("Error fetching Epics: " + response.Content, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void FetchEpicDetails(string project, string epicId)
        {
            var client = new RestClient($"https://dev.azure.com/{organization}/{project}/_apis/wit/workitems/{epicId}?$expand=relations&api-version=6.0");

            var request = new RestRequest
            {
                Method = Method.Get
            };
            request.AddHeader("Authorization", $"Basic {Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($":{personalAccessToken}"))}");

            RestResponse response = client.Execute(request);

            if (response.IsSuccessful)
            {
                dynamic workItemDetails = JsonConvert.DeserializeObject(response.Content);

                // Get the Epic State value
                string epicState = workItemDetails.fields.ContainsKey("System.State") ? workItemDetails.fields["System.State"].ToString() : "N/A";

                // Get the Epic Title
                string title = workItemDetails.fields.ContainsKey("System.Title") ? workItemDetails.fields["System.Title"].ToString() : "N/A";

                // Get the total story points from child work items
                double totalStoryPoints = FetchChildStoryPoints(project, workItemDetails);

                // Get Effort value
                double effort = workItemDetails.fields.ContainsKey("Microsoft.VSTS.Scheduling.Effort") ? (double)workItemDetails.fields["Microsoft.VSTS.Scheduling.Effort"] : 0;

                // Debug: log effort and story points
                Console.WriteLine($"Effort: {effort}, Total Story Points: {totalStoryPoints}");

                // Calculate the difference between Effort and Story Points
                double difference = effort - totalStoryPoints;

                // Debug: log the difference
                Console.WriteLine($"Difference: {difference}");

                // Add columns to DataGridView if not already done
                if (dataGridView.Columns.Count == 0)
                {
                    dataGridView.Columns.Add("EpicID", "Epic ID");
                    dataGridView.Columns.Add("EpicState", "Epic State");
                    dataGridView.Columns.Add("Title", "Title");
                    dataGridView.Columns.Add("Effort", "Effort");
                    dataGridView.Columns.Add("StoryPoints", "Story Points");
                    dataGridView.Columns.Add("Difference", "Effort - Story Points");
                }
                //MessageBox.Show($"Epic details for Epic ID: {epicId} fetched successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Add Epic details and the calculated difference to the DataGridView
                dataGridView.Rows.Add(epicId, epicState, title, effort, totalStoryPoints, difference);

                // Auto-resize columns after adding data
                dataGridView.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);
            }
            else
            {
                MessageBox.Show($"Error fetching Epic details: {response.Content}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }



        private double FetchChildStoryPoints(string project, dynamic epicDetails)
        {
            double totalStoryPoints = 0;

            if (epicDetails.relations != null)
            {
                foreach (var relation in epicDetails.relations)
                {
                    if (relation.rel == "System.LinkTypes.Hierarchy-Forward")
                    {
                        string childUrl = relation.url.ToString();
                        string childId = childUrl.Split('/').Last();

                        var client = new RestClient($"https://dev.azure.com/{organization}/{project}/_apis/wit/workitems/{childId}?api-version=6.0");

                        var request = new RestRequest
                        {
                            Method = Method.Get
                        };
                        request.AddHeader("Authorization", $"Basic {Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($":{personalAccessToken}"))}");

                        RestResponse response = client.Execute(request);

                        if (response.IsSuccessful)
                        {
                            dynamic childDetails = JsonConvert.DeserializeObject(response.Content);
                            if (childDetails.fields.ContainsKey("Microsoft.VSTS.Scheduling.StoryPoints"))
                            {
                                totalStoryPoints += (double)childDetails.fields["Microsoft.VSTS.Scheduling.StoryPoints"];
                            }
                        }
                    }
                }
            }

            return totalStoryPoints;
        }
    }
}

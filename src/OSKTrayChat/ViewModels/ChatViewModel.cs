

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows.Forms;

namespace OSKTrayChat.ViewModels
{
    public class ChatContext
    {
        public string FilePath { get; set; }
        public string FileContents { get; set; }
        public string FileName => $"File:/{Path.GetFileName(FilePath)}";

        public ChatContext(string filePath, string fileContents)
        {
            FilePath = filePath;
            FileContents = fileContents;
        }
    }
    public class ChatViewModel : INotifyPropertyChanged
    {
        public void Init()
        {
            this.Context = new ObservableCollection<ChatContext>() {
            };
        }
        public ChatViewModel()
        {
            
        }

        public void SelectOpenFile()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            // Set filter for file extension and default file extension
            openFileDialog.DefaultExt = ".*";
            openFileDialog.Filter = "All files (*.*)|*.*"; 

            // Display OpenFileDialog by calling ShowDialog method
            var result = openFileDialog.ShowDialog();

            // If a file is selected
            if (result == System.Windows.Forms.DialogResult.OK)
            {    // Get the selected file name and read the file
                string filePath = openFileDialog.FileName;

                // Read the contents of the file into a string
                string fileContent = System.IO.File.ReadAllText(filePath);

                this.context.Add(new ChatContext(filePath, fileContent));
            }
        }


        private ObservableCollection<ChatContext> context;

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public ObservableCollection<ChatContext> Context
        {
            get => context;
            set
            {
                if (context != value)
                {
                    context = value;
                    OnPropertyChanged(nameof(Context));
                }
            }
        }
    }
}

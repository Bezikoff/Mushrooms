using System.Collections.ObjectModel;

namespace Mushrooms
{
    public partial class MainPage : ContentPage
    {
        ObservableCollection<Mushroom> mushrooms = new();
        Mushroom selectedMushroom = null;
        bool isEditing = false;
        string copiedImageFileName = null;
        const string CsvPath = "mushrooms.csv";

        public MainPage()
        {
            InitializeComponent();
            LoadData();
            ListPage.ItemsSource = mushrooms;
        }

        void LoadData()
        {
            mushrooms.Clear();
            if (!File.Exists(CsvPath)) return;

            foreach (var line in File.ReadAllLines(CsvPath))
            {
                var parts = line.Split(';');
                if (parts.Length >= 4)
                {
                    mushrooms.Add(new Mushroom
                    {
                        Name = parts[0],
                        LatinName = parts[1],
                        Description = parts[2],
                        ImageFileName = parts[3],
                        ShortDescription = parts[2].Length > 50 ? parts[2].Substring(0, 50) + "..." : parts[2]
                    });
                }
            }
        }

        void SaveData()
        {
            File.WriteAllLines(CsvPath, mushrooms.Select(m =>
                $"{m.Name};{m.LatinName};{m.Description};{m.ImageFileName}"));
        }

        void ShowPage(string page)
        {
            ListPage.IsVisible = page == "list";
            DetailPage.IsVisible = page == "detail";
            EditPage.IsVisible = page == "edit";
            PageTitle.Text = page switch
            {
                "list" => "Список грибов",
                "detail" => "Подробнее",
                "edit" => "Редактирование",
                _ => ""
            };

            // Показываем кнопку удаления только если редактируется существующий объект
            DeleteButton.IsVisible = page == "edit" && selectedMushroom != null;
        }

        void OnDescriptionClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is Mushroom m)
            {
                selectedMushroom = m;
                DetailName.Text = m.Name;
                DetailLatinName.Text = m.LatinName;
                DetailDescription.Text = m.Description;
                DetailImage.Source = m.FullImagePath;
                ShowPage("detail");
            }
        }

        void OnBackClicked(object sender, EventArgs e)
        {
            selectedMushroom = null;
            copiedImageFileName = null;
            isEditing = false;
            ShowPage("list");
        }

        void OnAddClicked(object sender, EventArgs e)
        {
            if (EditPage.IsVisible)
            {
                DisplayAlert("Ошибка", "Уже открыта страница редактирования", "OK");
                return;
            }

            selectedMushroom = null;
            copiedImageFileName = null;
            isEditing = true;
            EditName.Text = "";
            EditLatinName.Text = "";
            EditDescription.Text = "";
            PickedImageLabel.Text = "Файл не выбран";
            ShowPage("edit");
        }

        void OnEditClicked(object sender, EventArgs e)
        {
            if (EditPage.IsVisible)
            {
                DisplayAlert("Ошибка", "Уже открыта страница редактирования", "OK");
                return;
            }

            isEditing = true;
            copiedImageFileName = selectedMushroom?.ImageFileName;
            EditName.Text = selectedMushroom.Name;
            EditLatinName.Text = selectedMushroom.LatinName;
            EditDescription.Text = selectedMushroom.Description;
            PickedImageLabel.Text = selectedMushroom.ImageFileName;
            ShowPage("edit");
        }

        async void OnPickImageClicked(object sender, EventArgs e)
        {
            try
            {
                var result = await FilePicker.PickAsync(new PickOptions { PickerTitle = "Выбери изображение" });
                if (result != null)
                {
                    string appFolder = FileSystem.AppDataDirectory;
                    copiedImageFileName = Path.GetFileName(result.FileName);
                    string destPath = Path.Combine(appFolder, copiedImageFileName);

                    using var sourceStream = await result.OpenReadAsync();
                    using var destStream = File.Create(destPath);
                    await sourceStream.CopyToAsync(destStream);

                    PickedImageLabel.Text = copiedImageFileName;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", ex.Message, "OK");
            }
        }

        void OnSaveClicked(object sender, EventArgs e)
        {
            string name = EditName.Text?.Trim();
            string latin = EditLatinName.Text?.Trim();
            string desc = EditDescription.Text?.Trim() ?? "";
            string imageName = copiedImageFileName ?? selectedMushroom?.ImageFileName ?? "";

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(latin))
            {
                DisplayAlert("Ошибка", "Название и латинское название обязательны", "OK");
                return;
            }

            if (selectedMushroom != null)
            {
                selectedMushroom.Name = name;
                selectedMushroom.LatinName = latin;
                selectedMushroom.Description = desc;
                selectedMushroom.ImageFileName = imageName;
                selectedMushroom.ShortDescription = desc.Length > 50 ? desc.Substring(0, 50) + "..." : desc;
            }
            else
            {
                mushrooms.Add(new Mushroom
                {
                    Name = name,
                    LatinName = latin,
                    Description = desc,
                    ImageFileName = imageName,
                    ShortDescription = desc.Length > 50 ? desc.Substring(0, 50) + "..." : desc
                });
            }

            SaveData();
            isEditing = false;
            ShowPage("list");
        }

        async void OnDeleteClicked(object sender, EventArgs e)
        {
            if (selectedMushroom != null)
            {
                bool confirm = await DisplayAlert("Удалить", $"Удалить «{selectedMushroom.Name}»?", "Да", "Нет");
                if (confirm)
                {
                    mushrooms.Remove(selectedMushroom);
                    selectedMushroom = null;
                    SaveData();
                    ShowPage("list");
                }
            }
        }
    }

    public class Mushroom
    {
        public string Name { get; set; }
        public string LatinName { get; set; }
        public string Description { get; set; }
        public string ImageFileName { get; set; }
        public string ShortDescription { get; set; }

        public string FullImagePath =>
            string.IsNullOrEmpty(ImageFileName)
                ? null
                : Path.Combine(FileSystem.AppDataDirectory, ImageFileName);
    }

}

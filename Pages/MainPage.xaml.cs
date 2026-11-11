using RoyalPlay.Models;
using RoyalPlay.PageModels;

namespace RoyalPlay.Pages
{
    public partial class MainPage : ContentPage
    {
        public MainPage(MainPageModel model)
        {
            InitializeComponent();
            BindingContext = model;
        }
    }
}
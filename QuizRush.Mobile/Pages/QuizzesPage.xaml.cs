using QuizRush.Core.ViewModels;
using QuizRush.Mobile.Services;
using Microsoft.Maui.Controls.Shapes;

namespace QuizRush.Mobile.Pages;

public partial class QuizzesPage : ContentPage
{
    private readonly AuthService _authService;
    private readonly QuizApiService _quizApiService;

    private List<QuizResponseViewModel> _existingQuizzes = [];
    private long? _editingQuizId;
    private QuizViewModel _model = CreateEmptyQuiz();

    public QuizzesPage(AuthService authService, QuizApiService quizApiService)
    {
        InitializeComponent();
        _authService = authService;
        _quizApiService = quizApiService;
        _authService.Session.SessionChanged += HandleSessionChanged;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await RefreshAsync();
    }

    private async void OnExistingQuizSelected(object? sender, EventArgs e)
    {
        if (ExistingQuizPicker.SelectedIndex < 0 || ExistingQuizPicker.SelectedIndex >= _existingQuizzes.Count)
        {
            return;
        }

        var selected = _existingQuizzes[ExistingQuizPicker.SelectedIndex];
        var quiz = await _quizApiService.GetQuizByIdAsync(selected.Id);
        if (quiz is null)
        {
            QuizStatusLabel.Text = "Could not load that quiz.";
            return;
        }

        _editingQuizId = quiz.Id;
        _model = new QuizViewModel
        {
            Title = quiz.Title,
            Description = quiz.Description,
            Questions = quiz.Questions.Select(q => new QuestionViewModel
            {
                Text = q.Text,
                PointsValue = q.PointsValue,
                TimeLimitSeconds = q.TimeLimitSeconds,
                Answers = q.Answers.Select(a => new AnswerViewModel
                {
                    Text = a.Text,
                    IsCorrect = a.IsCorrect
                }).ToList()
            }).ToList()
        };

        PopulateFormFromModel();
        QuizStatusLabel.Text = $"Editing \"{quiz.Title}\".";
    }

    private void OnCreateNewClicked(object? sender, EventArgs e)
    {
        ResetToNewQuizForm("Creating a new quiz.");
    }

    private void OnAddQuestionClicked(object? sender, EventArgs e)
    {
        SyncModelFromUi();
        _model.Questions.Add(NewQuestion());
        PopulateFormFromModel();
    }

    private async void OnSaveQuizClicked(object? sender, EventArgs e)
    {
        try
        {
            SyncModelFromUi();
            ValidateModel(_model);

            var savedTitle = _model.Title;
            if (_editingQuizId.HasValue)
            {
                await _quizApiService.UpdateQuizAsync(_editingQuizId.Value, _model);
                await RefreshQuizListAsync();
                ResetToNewQuizForm($"Saved \"{savedTitle}\". Pick a quiz to edit or create a new one.");
            }
            else
            {
                await _quizApiService.CreateQuizAsync(_model);
                await RefreshQuizListAsync();
                ResetToNewQuizForm($"Created \"{savedTitle}\". Pick a quiz to edit or create a new one.");
            }
        }
        catch (Exception ex)
        {
            QuizStatusLabel.Text = ex.Message;
        }
    }

    private async Task RefreshAsync()
    {
        var isAuthenticated = _authService.Session.IsAuthenticated;
        QuizzesLoggedOutPanel.IsVisible = !isAuthenticated;
        QuizzesLoggedInPanel.IsVisible = isAuthenticated;

        if (!isAuthenticated)
        {
            QuizStatusLabel.Text = "Log in to manage quizzes.";
            return;
        }

        try
        {
            await RefreshQuizListAsync();
            if (!_editingQuizId.HasValue)
            {
                ResetToNewQuizForm(string.IsNullOrWhiteSpace(QuizStatusLabel.Text)
                    ? "Select a quiz to edit or create a new one."
                    : QuizStatusLabel.Text);
            }
        }
        catch (Exception ex)
        {
            QuizStatusLabel.Text = ex.Message;
        }
    }

    private async Task RefreshQuizListAsync()
    {
        _existingQuizzes = await _quizApiService.GetMyQuizzesAsync();
        ExistingQuizPicker.ItemsSource = _existingQuizzes.Select(q => q.Title).ToList();
    }

    private void ResetToNewQuizForm(string statusMessage)
    {
        _editingQuizId = null;
        _model = CreateEmptyQuiz();
        ExistingQuizPicker.SelectedIndex = -1;
        PopulateFormFromModel();
        QuizStatusLabel.Text = statusMessage;
    }

    private void PopulateFormFromModel()
    {
        TitleEntry.Text = _model.Title;
        DescriptionEditor.Text = _model.Description;
        QuestionCountLabel.Text = $"Questions: {_model.Questions.Count}";
        QuestionsLayout.Children.Clear();

        for (var qi = 0; qi < _model.Questions.Count; qi++)
        {
            var question = _model.Questions[qi];
            var container = new VerticalStackLayout { Spacing = 8 };
            container.Children.Add(new Label
            {
                Text = $"Question {qi + 1}",
                FontAttributes = FontAttributes.Bold,
                FontSize = 18,
                TextColor = Color.FromArgb("#101828")
            });

            var text = new Editor
            {
                Text = question.Text,
                AutoSize = EditorAutoSizeOption.TextChanges,
                HeightRequest = 90,
                BackgroundColor = Colors.White,
                TextColor = Color.FromArgb("#101828"),
                Placeholder = "Question text"
            };
            var points = new Entry
            {
                Text = question.PointsValue.ToString(),
                Keyboard = Keyboard.Numeric,
                Placeholder = "Points",
                BackgroundColor = Colors.White,
                TextColor = Color.FromArgb("#101828")
            };
            var time = new Entry
            {
                Text = question.TimeLimitSeconds.ToString(),
                Keyboard = Keyboard.Numeric,
                Placeholder = "Seconds",
                BackgroundColor = Colors.White,
                TextColor = Color.FromArgb("#101828")
            };
            container.Children.Add(text);
            container.Children.Add(points);
            container.Children.Add(time);

            for (var ai = 0; ai < question.Answers.Count; ai++)
            {
                var answer = question.Answers[ai];
                var row = new HorizontalStackLayout { Spacing = 8 };
                var radio = new RadioButton
                {
                    IsChecked = answer.IsCorrect,
                    GroupName = $"q_{qi}",
                    Value = $"answer_{ai}"
                };
                var answerEntry = new Entry
                {
                    Text = answer.Text,
                    HorizontalOptions = LayoutOptions.Fill,
                    BackgroundColor = Colors.White,
                    TextColor = Color.FromArgb("#101828"),
                    Placeholder = $"Answer {ai + 1}"
                };
                row.Children.Add(radio);
                row.Children.Add(answerEntry);
                container.Children.Add(row);
            }

            QuestionsLayout.Children.Add(new Border
            {
                StrokeThickness = 0,
                Background = Color.FromArgb("#FFFFFF"),
                Padding = 14,
                Margin = new Thickness(0, 0, 0, 6),
                StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(20) },
                Content = container
            });
        }
    }

    private void SyncModelFromUi()
    {
        _model.Title = TitleEntry.Text?.Trim() ?? string.Empty;
        _model.Description = DescriptionEditor.Text?.Trim() ?? string.Empty;

        for (var qi = 0; qi < _model.Questions.Count && qi < QuestionsLayout.Children.Count; qi++)
        {
            if (QuestionsLayout.Children[qi] is not Border border || border.Content is not VerticalStackLayout stack)
            {
                continue;
            }

            _model.Questions[qi].Text = (stack.Children[1] as Editor)?.Text?.Trim() ?? string.Empty;
            _model.Questions[qi].PointsValue = int.TryParse((stack.Children[2] as Entry)?.Text, out var points) ? points : 100;
            _model.Questions[qi].TimeLimitSeconds = int.TryParse((stack.Children[3] as Entry)?.Text, out var seconds) ? seconds : 20;

            for (var ai = 0; ai < _model.Questions[qi].Answers.Count; ai++)
            {
                if (stack.Children[4 + ai] is not HorizontalStackLayout row)
                {
                    continue;
                }

                var radio = row.Children[0] as RadioButton;
                var entry = row.Children[1] as Entry;
                _model.Questions[qi].Answers[ai].IsCorrect = radio?.IsChecked ?? false;
                _model.Questions[qi].Answers[ai].Text = entry?.Text?.Trim() ?? string.Empty;
            }
        }
    }

    private static void ValidateModel(QuizViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Title) || model.Title.Length < 3)
        {
            throw new InvalidOperationException("Title must be at least 3 characters.");
        }

        foreach (var question in model.Questions)
        {
            if (string.IsNullOrWhiteSpace(question.Text))
            {
                throw new InvalidOperationException("Each question needs text.");
            }

            if (question.Answers.Count < 2)
            {
                throw new InvalidOperationException("Each question needs at least two answers.");
            }

            if (question.Answers.Any(a => string.IsNullOrWhiteSpace(a.Text)))
            {
                throw new InvalidOperationException("Fill in every answer text.");
            }

            if (!question.Answers.Any(a => a.IsCorrect))
            {
                throw new InvalidOperationException("Each question must have one correct answer.");
            }
        }
    }

    private static QuizViewModel CreateEmptyQuiz()
    {
        return new QuizViewModel
        {
            Questions = [NewQuestion()]
        };
    }

    private static QuestionViewModel NewQuestion()
    {
        return new QuestionViewModel
        {
            PointsValue = 100,
            TimeLimitSeconds = 20,
            Answers =
            [
                new AnswerViewModel { Text = string.Empty, IsCorrect = true },
                new AnswerViewModel { Text = string.Empty, IsCorrect = false }
            ]
        };
    }

    private void HandleSessionChanged()
    {
        MainThread.BeginInvokeOnMainThread(async () => await RefreshAsync());
    }
}

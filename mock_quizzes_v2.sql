-- ============================================================
-- QuizRush Mock Data Script v2 - Fresh & Complete
-- Uses dynamic IDs and handles re-runs gracefully
-- ============================================================

-- Get a valid CreatorId (uses first user found, or fails clearly)
DECLARE @CreatorId BIGINT;
SELECT TOP 1 @CreatorId = Id FROM Users ORDER BY Id;

IF @CreatorId IS NULL
BEGIN
    PRINT 'ERROR: No users found. Please register at least one user first.';
    RETURN;
END

PRINT 'Using CreatorId: ' + CAST(@CreatorId AS NVARCHAR);

-- ============================================================
-- QUIZ 1: Science Basics
-- ============================================================
DECLARE @Quiz1Id BIGINT;
INSERT INTO Quizzes (Title, Description, CreatorId, CreatedAt)
VALUES ('Science Basics', 'Test your knowledge of fundamental science concepts', @CreatorId, GETUTCDATE());
SET @Quiz1Id = SCOPE_IDENTITY();

DECLARE @Q1 BIGINT, @Q2 BIGINT, @Q3 BIGINT, @Q4 BIGINT;

INSERT INTO Questions (Text, QuizId, PointsValue, TimeLimitSeconds)
VALUES ('What is the chemical symbol for Gold?', @Quiz1Id, 100, 15);
SET @Q1 = SCOPE_IDENTITY();
INSERT INTO Answers (Text, QuestionId, IsCorrect) VALUES
    ('Au', @Q1, 1), ('Ag', @Q1, 0), ('Go', @Q1, 0), ('Gd', @Q1, 0);

INSERT INTO Questions (Text, QuizId, PointsValue, TimeLimitSeconds)
VALUES ('Which planet is closest to the Sun?', @Quiz1Id, 100, 15);
SET @Q2 = SCOPE_IDENTITY();
INSERT INTO Answers (Text, QuestionId, IsCorrect) VALUES
    ('Mercury', @Q2, 1), ('Venus', @Q2, 0), ('Earth', @Q2, 0), ('Mars', @Q2, 0);

INSERT INTO Questions (Text, QuizId, PointsValue, TimeLimitSeconds)
VALUES ('What is the powerhouse of the cell?', @Quiz1Id, 100, 15);
SET @Q3 = SCOPE_IDENTITY();
INSERT INTO Answers (Text, QuestionId, IsCorrect) VALUES
    ('Mitochondria', @Q3, 1), ('Nucleus', @Q3, 0), ('Ribosome', @Q3, 0), ('Chloroplast', @Q3, 0);

INSERT INTO Questions (Text, QuizId, PointsValue, TimeLimitSeconds)
VALUES ('How many bones does an adult human have?', @Quiz1Id, 100, 20);
SET @Q4 = SCOPE_IDENTITY();
INSERT INTO Answers (Text, QuestionId, IsCorrect) VALUES
    ('206', @Q4, 1), ('216', @Q4, 0), ('186', @Q4, 0), ('226', @Q4, 0);

-- ============================================================
-- QUIZ 2: World History
-- ============================================================
DECLARE @Quiz2Id BIGINT;
INSERT INTO Quizzes (Title, Description, CreatorId, CreatedAt)
VALUES ('World History', 'Journey through the ages with these historical questions', @CreatorId, GETUTCDATE());
SET @Quiz2Id = SCOPE_IDENTITY();

DECLARE @Q5 BIGINT, @Q6 BIGINT, @Q7 BIGINT;

INSERT INTO Questions (Text, QuizId, PointsValue, TimeLimitSeconds)
VALUES ('In which year did the Titanic sink?', @Quiz2Id, 150, 20);
SET @Q5 = SCOPE_IDENTITY();
INSERT INTO Answers (Text, QuestionId, IsCorrect) VALUES
    ('1912', @Q5, 1), ('1905', @Q5, 0), ('1920', @Q5, 0), ('1898', @Q5, 0);

INSERT INTO Questions (Text, QuizId, PointsValue, TimeLimitSeconds)
VALUES ('Who was the first President of the United States?', @Quiz2Id, 150, 20);
SET @Q6 = SCOPE_IDENTITY();
INSERT INTO Answers (Text, QuestionId, IsCorrect) VALUES
    ('George Washington', @Q6, 1), ('Thomas Jefferson', @Q6, 0),
    ('Benjamin Franklin', @Q6, 0), ('John Adams', @Q6, 0);

INSERT INTO Questions (Text, QuizId, PointsValue, TimeLimitSeconds)
VALUES ('Which empire built Machu Picchu?', @Quiz2Id, 150, 20);
SET @Q7 = SCOPE_IDENTITY();
INSERT INTO Answers (Text, QuestionId, IsCorrect) VALUES
    ('Inca', @Q7, 1), ('Aztec', @Q7, 0), ('Maya', @Q7, 0), ('Olmec', @Q7, 0);

-- ============================================================
-- QUIZ 3: Sports Trivia
-- ============================================================
DECLARE @Quiz3Id BIGINT;
INSERT INTO Quizzes (Title, Description, CreatorId, CreatedAt)
VALUES ('Sports Trivia', 'Test your knowledge of sports around the world', @CreatorId, GETUTCDATE());
SET @Quiz3Id = SCOPE_IDENTITY();

DECLARE @Q8 BIGINT, @Q9 BIGINT, @Q10 BIGINT, @Q11 BIGINT;

INSERT INTO Questions (Text, QuizId, PointsValue, TimeLimitSeconds)
VALUES ('How many players are on a basketball team court at once?', @Quiz3Id, 100, 15);
SET @Q8 = SCOPE_IDENTITY();
INSERT INTO Answers (Text, QuestionId, IsCorrect) VALUES
    ('5', @Q8, 1), ('6', @Q8, 0), ('7', @Q8, 0), ('4', @Q8, 0);

INSERT INTO Questions (Text, QuizId, PointsValue, TimeLimitSeconds)
VALUES ('Which country has won the most FIFA World Cups?', @Quiz3Id, 100, 15);
SET @Q9 = SCOPE_IDENTITY();
INSERT INTO Answers (Text, QuestionId, IsCorrect) VALUES
    ('Brazil', @Q9, 1), ('Germany', @Q9, 0), ('France', @Q9, 0), ('Italy', @Q9, 0);

INSERT INTO Questions (Text, QuizId, PointsValue, TimeLimitSeconds)
VALUES ('In tennis, what is a score of zero called?', @Quiz3Id, 100, 15);
SET @Q10 = SCOPE_IDENTITY();
INSERT INTO Answers (Text, QuestionId, IsCorrect) VALUES
    ('Love', @Q10, 1), ('Zero', @Q10, 0), ('Nothing', @Q10, 0), ('Nil', @Q10, 0);

INSERT INTO Questions (Text, QuizId, PointsValue, TimeLimitSeconds)
VALUES ('How long is an Olympic swimming pool?', @Quiz3Id, 100, 20);
SET @Q11 = SCOPE_IDENTITY();
INSERT INTO Answers (Text, QuestionId, IsCorrect) VALUES
    ('50 meters', @Q11, 1), ('25 meters', @Q11, 0),
    ('75 meters', @Q11, 0), ('100 meters', @Q11, 0);

-- ============================================================
-- QUIZ 4: Programming Fundamentals
-- ============================================================
DECLARE @Quiz4Id BIGINT;
INSERT INTO Quizzes (Title, Description, CreatorId, CreatedAt)
VALUES ('Programming Fundamentals', 'Basics of coding and software development', @CreatorId, GETUTCDATE());
SET @Quiz4Id = SCOPE_IDENTITY();

DECLARE @Q12 BIGINT, @Q13 BIGINT, @Q14 BIGINT;

INSERT INTO Questions (Text, QuizId, PointsValue, TimeLimitSeconds)
VALUES ('What does HTML stand for?', @Quiz4Id, 80, 15);
SET @Q12 = SCOPE_IDENTITY();
INSERT INTO Answers (Text, QuestionId, IsCorrect) VALUES
    ('HyperText Markup Language', @Q12, 1),
    ('High Tech Modern Language', @Q12, 0),
    ('Home Tool Markup Language', @Q12, 0),
    ('Hyperlinks and Text Markup Language', @Q12, 0);

INSERT INTO Questions (Text, QuizId, PointsValue, TimeLimitSeconds)
VALUES ('Which of these is a programming language?', @Quiz4Id, 80, 15);
SET @Q13 = SCOPE_IDENTITY();
INSERT INTO Answers (Text, QuestionId, IsCorrect) VALUES
    ('Python', @Q13, 1), ('HTML', @Q13, 0), ('CSS', @Q13, 0), ('MySQL', @Q13, 0);

INSERT INTO Questions (Text, QuizId, PointsValue, TimeLimitSeconds)
VALUES ('What is the main purpose of CSS?', @Quiz4Id, 80, 15);
SET @Q14 = SCOPE_IDENTITY();
INSERT INTO Answers (Text, QuestionId, IsCorrect) VALUES
    ('Add styling to web pages', @Q14, 1),
    ('Create database structures', @Q14, 0),
    ('Build server logic', @Q14, 0),
    ('Manage user authentication', @Q14, 0);

-- ============================================================
-- QUIZ 5: Geography Challenge
-- ============================================================
DECLARE @Quiz5Id BIGINT;
INSERT INTO Quizzes (Title, Description, CreatorId, CreatedAt)
VALUES ('Geography Challenge', 'Explore the world with geography questions', @CreatorId, GETUTCDATE());
SET @Quiz5Id = SCOPE_IDENTITY();

DECLARE @Q15 BIGINT, @Q16 BIGINT, @Q17 BIGINT, @Q18 BIGINT;

INSERT INTO Questions (Text, QuizId, PointsValue, TimeLimitSeconds)
VALUES ('What is the capital of Japan?', @Quiz5Id, 120, 15);
SET @Q15 = SCOPE_IDENTITY();
INSERT INTO Answers (Text, QuestionId, IsCorrect) VALUES
    ('Tokyo', @Q15, 1), ('Osaka', @Q15, 0), ('Kyoto', @Q15, 0), ('Yokohama', @Q15, 0);

INSERT INTO Questions (Text, QuizId, PointsValue, TimeLimitSeconds)
VALUES ('Which is the largest continent by area?', @Quiz5Id, 120, 15);
SET @Q16 = SCOPE_IDENTITY();
INSERT INTO Answers (Text, QuestionId, IsCorrect) VALUES
    ('Asia', @Q16, 1), ('Africa', @Q16, 0),
    ('Europe', @Q16, 0), ('North America', @Q16, 0);

INSERT INTO Questions (Text, QuizId, PointsValue, TimeLimitSeconds)
VALUES ('How many countries are in the European Union?', @Quiz5Id, 120, 20);
SET @Q17 = SCOPE_IDENTITY();
INSERT INTO Answers (Text, QuestionId, IsCorrect) VALUES
    ('27', @Q17, 1), ('25', @Q17, 0), ('30', @Q17, 0), ('28', @Q17, 0);

INSERT INTO Questions (Text, QuizId, PointsValue, TimeLimitSeconds)
VALUES ('What is the longest river in the world?', @Quiz5Id, 120, 20);
SET @Q18 = SCOPE_IDENTITY();
INSERT INTO Answers (Text, QuestionId, IsCorrect) VALUES
    ('Nile River', @Q18, 1), ('Amazon River', @Q18, 0),
    ('Yangtze River', @Q18, 0), ('Mississippi River', @Q18, 0);

-- ============================================================
-- Verification
-- ============================================================
SELECT
    q.Id,
    q.Title,
    COUNT(DISTINCT qn.Id) AS Questions,
    COUNT(a.Id) AS TotalAnswers
FROM Quizzes q
LEFT JOIN Questions qn ON q.Id = qn.QuizId
LEFT JOIN Answers a ON qn.Id = a.QuestionId
GROUP BY q.Id, q.Title
ORDER BY q.Id;

PRINT 'Mock data loaded successfully!';

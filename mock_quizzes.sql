-- ============================================================
-- QuizRush Test Data SQL Script
-- This script inserts 3 test users and 5 sample quizzes
-- ============================================================

-- ============================================================
-- INSERT TEST USERS
-- ============================================================
-- Note: Password hashes are placeholders. Use proper bcrypt hashes in production.
-- Passwords: testuser1, testuser2, testuser3 (all same for testing)

INSERT INTO Users (Username, Email, PasswordHash, PasswordSalt, IsAdmin, AccumulatedPoints, CreatedAt)
VALUES
    (
        'testuser1',
        'testuser1@example.com',
        '$2a$11$abcdefghijklmnopqrstuvwxyz1234567890abcdefghijklmnopqrstuv', -- placeholder hash
        'salt123',
        0,
        0,
        GETUTCDATE()
    ),
    (
        'testuser2',
        'testuser2@example.com',
        '$2a$11$abcdefghijklmnopqrstuvwxyz1234567890abcdefghijklmnopqrstuv', -- placeholder hash
        'salt123',
        0,
        0,
        GETUTCDATE()
    ),
    (
        'testuser3',
        'testuser3@example.com',
        '$2a$11$abcdefghijklmnopqrstuvwxyz1234567890abcdefghijklmnopqrstuv', -- placeholder hash
        'salt123',
        0,
        0,
        GETUTCDATE()
    );

-- ============================================================
-- QUIZ 1: Science Basics
-- ============================================================
INSERT INTO Quizzes (Title, Description, CreatorId, CreatedAt)
VALUES ('Science Basics', 'Test your knowledge of fundamental science concepts', 1, GETUTCDATE());

INSERT INTO Questions (Text, QuizId, PointsValue, TimeLimitSeconds)
VALUES
    ('What is the chemical symbol for Gold?', 1, 100, 15),
    ('Which planet is closest to the Sun?', 1, 100, 15),
    ('What is the powerhouse of the cell?', 1, 100, 15),
    ('How many bones does an adult human have?', 1, 100, 20);

INSERT INTO Answers (Text, QuestionId, IsCorrect)
VALUES
    ('Au', 1, 1),
    ('Ag', 1, 0),
    ('Go', 1, 0),
    ('Gd', 1, 0),

    ('Mercury', 2, 1),
    ('Venus', 2, 0),
    ('Earth', 2, 0),
    ('Mars', 2, 0),

    ('Mitochondria', 3, 1),
    ('Nucleus', 3, 0),
    ('Ribosome', 3, 0),
    ('Chloroplast', 3, 0),

    ('206', 4, 1),
    ('206', 4, 1),
    ('186', 4, 0),
    ('226', 4, 0);

-- ============================================================
-- QUIZ 2: World History
-- ============================================================
INSERT INTO Quizzes (Title, Description, CreatorId, CreatedAt)
VALUES ('World History', 'Journey through the ages with these historical questions', 2, GETUTCDATE());

INSERT INTO Questions (Text, QuizId, PointsValue, TimeLimitSeconds)
VALUES
    ('In which year did the Titanic sink?', 2, 150, 20),
    ('Who was the first President of the United States?', 2, 150, 20),
    ('Which empire built Machu Picchu?', 2, 150, 20);

INSERT INTO Answers (Text, QuestionId, IsCorrect)
VALUES
    ('1912', 5, 1),
    ('1905', 5, 0),
    ('1920', 5, 0),
    ('1898', 5, 0),

    ('George Washington', 6, 1),
    ('Thomas Jefferson', 6, 0),
    ('Benjamin Franklin', 6, 0),
    ('John Adams', 6, 0),

    ('Inca', 7, 1),
    ('Aztec', 7, 0),
    ('Maya', 7, 0),
    ('Olmec', 7, 0);

-- ============================================================
-- QUIZ 3: Sports Trivia
-- ============================================================
INSERT INTO Quizzes (Title, Description, CreatorId, CreatedAt)
VALUES ('Sports Trivia', 'Test your knowledge of sports around the world', 3, GETUTCDATE());

INSERT INTO Questions (Text, QuizId, PointsValue, TimeLimitSeconds)
VALUES
    ('How many players are on a basketball team court at once?', 3, 100, 15),
    ('Which country has won the most FIFA World Cups?', 3, 100, 15),
    ('In tennis, what is a score of zero called?', 3, 100, 15),
    ('How long is an Olympic swimming pool?', 3, 100, 20);

INSERT INTO Answers (Text, QuestionId, IsCorrect)
VALUES
    ('5', 8, 1),
    ('6', 8, 0),
    ('7', 8, 0),
    ('4', 8, 0),

    ('Brazil', 9, 1),
    ('Germany', 9, 0),
    ('France', 9, 0),
    ('Italy', 9, 0),

    ('Love', 10, 1),
    ('Zero', 10, 0),
    ('Nothing', 10, 0),
    ('Nil', 10, 0),

    ('50 meters', 11, 1),
    ('25 meters', 11, 0),
    ('75 meters', 11, 0),
    ('100 meters', 11, 0);

-- ============================================================
-- QUIZ 4: Programming Fundamentals
-- ============================================================
INSERT INTO Quizzes (Title, Description, CreatorId, CreatedAt)
VALUES ('Programming Fundamentals', 'Basics of coding and software development', 1, GETUTCDATE());

INSERT INTO Questions (Text, QuizId, PointsValue, TimeLimitSeconds)
VALUES
    ('What does HTML stand for?', 4, 80, 15),
    ('Which of these is a programming language?', 4, 80, 15),
    ('What is the main purpose of CSS?', 4, 80, 15);

INSERT INTO Answers (Text, QuestionId, IsCorrect)
VALUES
    ('HyperText Markup Language', 12, 1),
    ('High Tech Modern Language', 12, 0),
    ('Home Tool Markup Language', 12, 0),
    ('Hyperlinks and Text Markup Language', 12, 0),

    ('Python', 13, 1),
    ('HTML', 13, 0),
    ('CSS', 13, 0),
    ('MySQL', 13, 0),

    ('Add styling to web pages', 14, 1),
    ('Create database structures', 14, 0),
    ('Build server logic', 14, 0),
    ('Manage user authentication', 14, 0);

-- ============================================================
-- QUIZ 5: Geography Challenge
-- ============================================================
INSERT INTO Quizzes (Title, Description, CreatorId, CreatedAt)
VALUES ('Geography Challenge', 'Explore the world with geography questions', 2, GETUTCDATE());

INSERT INTO Questions (Text, QuizId, PointsValue, TimeLimitSeconds)
VALUES
    ('What is the capital of Japan?', 5, 120, 15),
    ('Which is the largest continent by area?', 5, 120, 15),
    ('How many countries are in the European Union?', 5, 120, 20),
    ('What is the longest river in the world?', 5, 120, 20);

INSERT INTO Answers (Text, QuestionId, IsCorrect)
VALUES
    ('Tokyo', 15, 1),
    ('Osaka', 15, 0),
    ('Kyoto', 15, 0),
    ('Yokohama', 15, 0),

    ('Asia', 16, 1),
    ('Africa', 16, 0),
    ('Europe', 16, 0),
    ('North America', 16, 0),

    ('27', 17, 1),
    ('25', 17, 0),
    ('30', 17, 0),
    ('28', 17, 0),

    ('Nile River', 18, 1),
    ('Amazon River', 18, 0),
    ('Yangtze River', 18, 0),
    ('Mississippi River', 18, 0);

-- ============================================================
-- Verification Queries (Run these to verify data was inserted)
-- ============================================================
/*
SELECT COUNT(*) as UserCount FROM Users;
SELECT COUNT(*) as QuizCount FROM Quizzes;
SELECT COUNT(*) as QuestionCount FROM Questions;
SELECT COUNT(*) as AnswerCount FROM Answers;

-- View all quizzes with question counts
SELECT q.Id, q.Title, q.Description, u.Username, COUNT(qn.Id) as QuestionCount
FROM Quizzes q
JOIN Users u ON q.CreatorId = u.Id
LEFT JOIN Questions qn ON q.Id = qn.QuizId
GROUP BY q.Id, q.Title, q.Description, u.Username
ORDER BY q.Id;
*/

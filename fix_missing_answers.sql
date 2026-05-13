-- Fix missing answers for questions 19 and 20
-- These questions had no answers inserted due to the original SQL script being partially executed

-- Question 19: How many countries are in the European Union?
INSERT INTO Answers (Text, QuestionId, IsCorrect)
VALUES
    ('27', 19, 1),
    ('25', 19, 0),
    ('30', 19, 0),
    ('28', 19, 0);

-- Question 20: What is the longest river in the world?
INSERT INTO Answers (Text, QuestionId, IsCorrect)
VALUES
    ('Nile River', 20, 1),
    ('Amazon River', 20, 0),
    ('Yangtze River', 20, 0),
    ('Mississippi River', 20, 0);

-- Verify the fix
SELECT q.Id, q.Text, COUNT(a.Id) as AnswerCount
FROM Questions q
LEFT JOIN Answers a ON a.QuestionId = q.Id
WHERE q.Id IN (19, 20)
GROUP BY q.Id, q.Text;

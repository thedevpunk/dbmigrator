-- Up
CREATE TABLE another_table
(
   id int PRIMARY KEY,
   name VARCHAR ( 50 ) UNIQUE NOT NULL,
   age int
);

-- Down
DROP TABLE another_table;

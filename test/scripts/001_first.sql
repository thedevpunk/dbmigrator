-- Up
CREATE TABLE test_table
(
   id int PRIMARY KEY,
   name VARCHAR ( 50 ) UNIQUE NOT NULL,
   age int
);

-- Down
DROP TABLE test_table;

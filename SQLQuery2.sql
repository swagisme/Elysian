-- Option A: Set NULL for invalid UserIds
UPDATE Carts 
SET UserId = NULL 
WHERE UserId IS NOT NULL 
  AND UserId NOT IN (SELECT Id FROM AspNetUsers);

-- Option B: Delete carts with invalid UserIds (if they're test data)
DELETE FROM Carts 
WHERE UserId IS NOT NULL 
  AND UserId NOT IN (SELECT Id FROM AspNetUsers);
select * from [dbo].[Machine08Samples]


;WITH NonNullRank AS
(
	SELECT SampleTimestamp, S2temp,  cnt = COUNT(s2temp) OVER (ORDER BY SampleTimestamp) 
	FROM Machine08Samples
),

WindowsWithNoValues AS
(
	SELECT SampleTimestamp, S2temp,  r = ROW_NUMBER() OVER (PARTITION BY cnt ORDER BY SampleTimestamp ASC) - 1
	FROM NonNullRank
)

SELECT SampleTimestamp, S2temp, S2tempWithValues= ISNULL(S2temp, LAG(S2temp, r) OVER (ORDER BY SampleTimestamp ASC))
FROM WindowsWithNoValues


;with NonNullRank AS
(
	SELECT SampleTimestamp, S2temp,  cnt = COUNT(s2temp) OVER (ORDER BY SampleTimestamp) 
	FROM Machine08Samples
	WHERE SampleTimestamp > '2018-05-19 06:39:51.0580240' and SampleTimestamp < '2018-05-19 06:40:02.7521710'
	),
	
WindowsWithNoValues AS
(
		SELECT SampleTimestamp, S2temp,  r = ROW_NUMBER() OVER (PARTITION BY cnt ORDER BY SampleTimestamp ASC) - 1
	FROM NonNullRank
	WHERE SampleTimestamp > '2018-05-19 06:39:51.0580240' and SampleTimestamp < '2018-05-19 06:40:02.7521710'
	)
	SELECT SampleTimestamp, S2temp, S2tempWithValues= ISNULL(S2temp, LAG(S2temp, r) OVER (ORDER BY SampleTimestamp ASC))
FROM WindowsWithNoValues



;WITH With30SecondBuckets AS
(
	SELECT * , (dateadd(second,(datediff(second,'2010-1-1',[SampleTimestamp])/(30))*(30),'2010-1-1')) AS  [SampleTimestamp30Seconds]
	FROM Machine08Samples
)

SELECT SampleTimestamp30Seconds, AVG(S2Temp) 
FROM With30SecondBuckets GROUP BY SampleTimestamp30Seconds
ORDER BY SampleTimestamp30Seconds

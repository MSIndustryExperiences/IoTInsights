; WITH Machine08SamplesTransposed AS 
(
	SELECT * FROM 
	(
		SELECT  SampleTimestamp, sampleName, CAST(sampleValue AS NUMERIC(20,3)) AS sampleValueNumeric 
		FROM Samples
		WHERE
			DeviceName = 'Mazak03' and ISNUMERIC(sampleValue) != 0
	) AS S
    
	PIVOT(
		MAX(sampleValueNumeric) 
		FOR SampleName IN ([S2temp],
			[Stemp],
			[Zabs],
			[Zfrt],
			[S2load],
			[Cfrt],
			[total_time],
			[Xabs],
			[Xload],
			[Fact],
			[Cload],
			[cut_time],
			[Zload],
			[S2rpm],
			[Srpm],
			[auto_time],
			[Cdeg],
			[Xfrt],
			[S1load])
		) AS PivotTable
		
)

SELECT * INTO Machine08Samples  
FROM Machine08SamplesTransposed

SELECT TOP 2000 * FROM Machine08Samples ORDER BY SampleTimestamp ASC



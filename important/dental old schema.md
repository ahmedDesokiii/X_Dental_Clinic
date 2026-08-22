```
USE [FAMILYDB.MDF]
GO
/****** Object:  Table [dbo].[Diagonsis_T]    Script Date: 2026-08-22 10:05:38
******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Diagonsis_T](
[Id] [int] IDENTITY(1,1) NOT NULL,
[dia_Name] [nvarchar](18) NULL,
[dia_Cost] [int] NULL,
[dia_Code] [int] NULL,
PRIMARY KEY CLUSTERED
(
[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF,
ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF)
ON [PRIMARY]
) ON [PRIMARY]
GO
```

```
USE [FAMILYDB.MDF]
GO
/****** Object:  Table [dbo].[Orthodontics_T]    Script Date: 2026-08-22
10:06:21 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Orthodontics_T](
[orth_Id] [int] IDENTITY(1,1) NOT NULL,
[orth_Code] [int] NULL,
[orth_Name] [nvarchar](100) NULL,
[orth_Age] [int] NULL,
[orth_Phone] [nvarchar](50) NULL,
[orth_Gender] [nvarchar](50) NULL,
[orth_Adress] [nvarchar](500) NULL,
[orth_Comments] [nvarchar](2000) NULL,
[orth_SessionCode] [int] NULL,
[orth_SessionDate] [date] NULL,
[orth_NextSessionDate] [date] NULL,
[orth_Follow] [nvarchar](50) NULL,
[orth_TotalAmount] [float] NULL,
[orth_RequiredAmount] [float] NULL,
[orth_PaidAmount] [float] NULL,
[orth_Discount] [float] NULL,
[orth_RemainingAmount]  AS ([orth_RequiredAmount]-([orth_PaidAmount]+
[orth_Discount])),
PRIMARY KEY CLUSTERED
(
[orth_Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF,
ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF)
ON [PRIMARY]
) ON [PRIMARY]
```

```
GO
```

```
USE [FAMILYDB.MDF]
GO
/****** Object:  Table [dbo].[OrthoRays_T]    Script Date: 2026-08-22 10:06:48
******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[OrthoRays_T](
[orRay_Id] [int] IDENTITY(1,1) NOT NULL,
[orRay_Code] [int] NULL,
[ortho_Code] [int] NULL,
[orRay_FileName] [nvarchar](100) NULL,
[orRay_Date] [date] NULL,
[orRay_Img] [image] NULL,
[orRay_SrcImg] [image] NULL,
[orRay_Notes] [nvarchar](1000) NULL
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
```

```
USE [FAMILYDB.MDF]
GO
/****** Object:  Table [dbo].[Patient_T]    Script Date: 2026-08-22 10:07:02
******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Patient_T](
[patient_Id] [int] IDENTITY(1,1) NOT NULL,
[patient_Code] [int] NULL,
[patient_Name] [nvarchar](100) NULL,
[patient_Age] [int] NULL,
[patient_Gender] [nvarchar](20) NULL,
[patient_Phone] [nvarchar](30) NULL,
[patient_Adress] [nvarchar](200) NULL,
[patient_SessionCode] [int] NULL,
[patient_SessionDate] [date] NULL,
[patient_SessionNextDate] [date] NULL,
[patient_MedicalHistory] [nvarchar](1000) NULL,
[patient_DiagonsisName] [nchar](100) NULL,
[patient_DiagonsisDetails] [nvarchar](1000) NULL,
[patient_DiagonsisCost] [float] NULL,
[patient_TeethChart] [nvarchar](50) NULL,
[patient_Follow] [nvarchar](50) NULL,
[patient_AmountRequired] [float] NULL,
[patient_AmountPaid] [float] NULL,
[patient_Discount] [float] NULL,
[paient_RemainingAmount]  AS ([patient_AmountRequired]-
([patient_AmountPaid]+[patient_Discount])),
```

```
PRIMARY KEY CLUSTERED
(
[patient_Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF,
ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF)
ON [PRIMARY]
) ON [PRIMARY]
GO
ALTER TABLE [dbo].[Patient_T] ADD  DEFAULT ((0)) FOR [patient_SessionCode]
GO
```

```
USE [FAMILYDB.MDF]
GO
```

```
/****** Object:  Table [dbo].[PatientRays_T]    Script Date: 2026-08-22 10:07:15
******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
```

```
CREATE TABLE [dbo].[PatientRays_T](
[paRay_Id] [int] IDENTITY(1,1) NOT NULL,
[paRay_Code] [int] NULL,
[patientt_Code] [int] NULL,
[paRay_Date] [date] NULL,
[paRay_FileName] [nvarchar](100) NULL,
[paRay_Img] [image] NULL,
[paRay_SrcImg] [image] NULL,
[paRay_Notes] [nvarchar](1000) NULL
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
```


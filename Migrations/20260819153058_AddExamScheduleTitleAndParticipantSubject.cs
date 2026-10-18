using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace schoolBE.Migrations
{
    /// <inheritdoc />
    public partial class AddExamScheduleTitleAndParticipantSubject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Title and SubjectId may already exist from a partial previous apply.
            migrationBuilder.Sql("""
                SET @exists := (
                    SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_SCHEMA = DATABASE()
                      AND TABLE_NAME = 'ExamSchedules'
                      AND COLUMN_NAME = 'Title');
                SET @sql := IF(@exists = 0,
                    'ALTER TABLE `ExamSchedules` ADD `Title` longtext CHARACTER SET utf8mb4 NOT NULL DEFAULT ''''',
                    'SELECT 1');
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
                """);

            migrationBuilder.Sql("""
                SET @exists := (
                    SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_SCHEMA = DATABASE()
                      AND TABLE_NAME = 'ExamParticipants'
                      AND COLUMN_NAME = 'SubjectId');
                SET @sql := IF(@exists = 0,
                    'ALTER TABLE `ExamParticipants` ADD `SubjectId` int NULL',
                    'SELECT 1');
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
                """);

            migrationBuilder.Sql("""
                SET @exists := (
                    SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS
                    WHERE TABLE_SCHEMA = DATABASE()
                      AND TABLE_NAME = 'ExamParticipants'
                      AND INDEX_NAME = 'IX_ExamParticipants_ExamScheduleId_StudentId_SubjectId');
                SET @sql := IF(@exists = 0,
                    'CREATE UNIQUE INDEX `IX_ExamParticipants_ExamScheduleId_StudentId_SubjectId` ON `ExamParticipants` (`ExamScheduleId`, `StudentId`, `SubjectId`)',
                    'SELECT 1');
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
                """);

            migrationBuilder.Sql("""
                SET @exists := (
                    SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS
                    WHERE TABLE_SCHEMA = DATABASE()
                      AND TABLE_NAME = 'ExamParticipants'
                      AND INDEX_NAME = 'IX_ExamParticipants_SubjectId');
                SET @sql := IF(@exists = 0,
                    'CREATE INDEX `IX_ExamParticipants_SubjectId` ON `ExamParticipants` (`SubjectId`)',
                    'SELECT 1');
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
                """);

            migrationBuilder.Sql("""
                SET @exists := (
                    SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
                    WHERE TABLE_SCHEMA = DATABASE()
                      AND TABLE_NAME = 'ExamParticipants'
                      AND CONSTRAINT_NAME = 'FK_ExamParticipants_Subjects_SubjectId');
                SET @sql := IF(@exists = 0,
                    'ALTER TABLE `ExamParticipants` ADD CONSTRAINT `FK_ExamParticipants_Subjects_SubjectId` FOREIGN KEY (`SubjectId`) REFERENCES `Subjects` (`Id`) ON DELETE SET NULL',
                    'SELECT 1');
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                SET @exists := (
                    SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
                    WHERE TABLE_SCHEMA = DATABASE()
                      AND TABLE_NAME = 'ExamParticipants'
                      AND CONSTRAINT_NAME = 'FK_ExamParticipants_Subjects_SubjectId');
                SET @sql := IF(@exists > 0,
                    'ALTER TABLE `ExamParticipants` DROP FOREIGN KEY `FK_ExamParticipants_Subjects_SubjectId`',
                    'SELECT 1');
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
                """);

            migrationBuilder.Sql("""
                SET @exists := (
                    SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS
                    WHERE TABLE_SCHEMA = DATABASE()
                      AND TABLE_NAME = 'ExamParticipants'
                      AND INDEX_NAME = 'IX_ExamParticipants_ExamScheduleId_StudentId_SubjectId');
                SET @sql := IF(@exists > 0,
                    'DROP INDEX `IX_ExamParticipants_ExamScheduleId_StudentId_SubjectId` ON `ExamParticipants`',
                    'SELECT 1');
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
                """);

            migrationBuilder.Sql("""
                SET @exists := (
                    SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS
                    WHERE TABLE_SCHEMA = DATABASE()
                      AND TABLE_NAME = 'ExamParticipants'
                      AND INDEX_NAME = 'IX_ExamParticipants_SubjectId');
                SET @sql := IF(@exists > 0,
                    'DROP INDEX `IX_ExamParticipants_SubjectId` ON `ExamParticipants`',
                    'SELECT 1');
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
                """);

            migrationBuilder.Sql("""
                SET @exists := (
                    SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_SCHEMA = DATABASE()
                      AND TABLE_NAME = 'ExamSchedules'
                      AND COLUMN_NAME = 'Title');
                SET @sql := IF(@exists > 0,
                    'ALTER TABLE `ExamSchedules` DROP COLUMN `Title`',
                    'SELECT 1');
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
                """);

            migrationBuilder.Sql("""
                SET @exists := (
                    SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_SCHEMA = DATABASE()
                      AND TABLE_NAME = 'ExamParticipants'
                      AND COLUMN_NAME = 'SubjectId');
                SET @sql := IF(@exists > 0,
                    'ALTER TABLE `ExamParticipants` DROP COLUMN `SubjectId`',
                    'SELECT 1');
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
                """);
        }
    }
}

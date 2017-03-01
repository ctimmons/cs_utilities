/* See the LICENSE.txt file in the root folder for license details. */

SET NOCOUNT ON;

DECLARE @client_items TABLE
  (
    [pathname] VARCHAR(MAX) NOT NULL DEFAULT '',
    [filename] VARCHAR(MAX) NOT NULL DEFAULT '',
    [schema_name] VARCHAR(MAX) NOT NULL,
    [object_name] VARCHAR(MAX) NOT NULL,
    /* [type]'s collation must match sys.objects(type)'s collation. */
    [type] CHAR(2) COLLATE Latin1_General_CI_AS_KS_WS,
    needs_to_be_compiled CHAR(1) NOT NULL DEFAULT 'N',
    is_present_on_server CHAR(1) NOT NULL,
    drop_order INT NOT NULL DEFAULT 0,
    does_file_exist CHAR(1) NOT NULL,
    needs_to_be_dropped CHAR(1) NOT NULL
  );

INSERT INTO @client_items
  (
    [pathname],
    [filename],
    [schema_name],
    [object_name],
    [type],
    needs_to_be_compiled,
    is_present_on_server,
    drop_order,
    does_file_exist,
    needs_to_be_dropped
  )
  VALUES
{0};

/* An UPDATE statement's SET operations act as if they're
   executed in parallel.  Therefore, some columns have
   to be set in a separate UPDATE before they can be referenced
   by later UPDATE statements. */

DECLARE @object_name_piece INT = 1;
DECLARE @schema_name_piece INT = 2;
DECLARE @default_schema_name VARCHAR(MAX) = '{1}';

UPDATE @client_items
  SET
    [schema_name] = COALESCE(PARSENAME([filename], @schema_name_piece), @default_schema_name),
    [object_name] = PARSENAME([filename], @object_name_piece);

UPDATE @client_items
  SET
    [type] =
      CASE
        WHEN [type] IS NULL THEN
          (
            /* User-defined table types */
            SELECT
                O.[type]
              FROM
                sys.table_types AS T
                INNER JOIN sys.objects AS O ON
                  O.[object_id] = T.type_table_object_id
                  AND T.[schema_id] = SCHEMA_ID([schema_name])
              WHERE
                [object_name] = T.[name]

            UNION

            /* Objects */
            SELECT
                O.[type]
              FROM
                sys.objects AS O
              WHERE
                O.[name] = [object_name]
                AND O.[schema_id] = SCHEMA_ID([schema_name])
          )
        ELSE [type]
      END,
    is_present_on_server = 
      CASE
        WHEN EXISTS
          (
            /* User-defined table types */
            SELECT
                *
              FROM
                sys.table_types AS T
                INNER JOIN sys.objects AS O ON
                  O.[object_id] = T.type_table_object_id
                  AND T.[schema_id] = SCHEMA_ID([schema_name])
              WHERE
                [object_name] = T.[name]
           )

           OR

           EXISTS
           (
            /* Objects */
            SELECT
                *
              FROM
                sys.objects AS O
              WHERE
                O.[name] = [object_name]
                AND O.[schema_id] = SCHEMA_ID([schema_name])
          ) THEN 'Y'
        ELSE 'N'
      END;

DECLARE @udtt_dependencies_to_drop TABLE
  (
    referencing_schema_name SYSNAME NOT NULL,
    referencing_object_name SYSNAME NOT NULL,
    /* [type]'s collation must match sys.objects(type)'s collation. */
    [type] CHAR(2) COLLATE Latin1_General_CI_AS_KS_WS
  );

WITH udtts_to_compile([schema_name], [object_name])
AS
(
  SELECT
      C.[schema_name],
      C.[object_name]
    FROM
      @client_items AS C
    WHERE
      C.[type] = 'TT'
      AND C.[needs_to_be_compiled] = 'Y'
),
udtt_dependencies_to_drop(referencing_id, referencing_schema_name, referencing_object_name, [type])
AS
(
  SELECT
      SED.referencing_id,
      referencing_schema_name = SCHEMA_NAME(O.schema_id),
      referencing_object_name = O.name,
      O.[type]
    FROM
      udtts_to_compile AS T
      INNER JOIN sys.sql_expression_dependencies AS SED ON SED.referenced_id = TYPE_ID(T.[schema_name] + '.' + T.[object_name])
      INNER JOIN sys.objects AS O ON SED.referencing_id = O.object_id
    WHERE
      SED.referenced_entity_name <> 'sysdiagrams'

  UNION ALL

  SELECT
      SED.referencing_id,
      SCHEMA_NAME(O.schema_id),
      O.name,
      O.[type]
    FROM
      udtt_dependencies_to_drop AS R
      INNER JOIN sys.sql_expression_dependencies AS SED ON R.referencing_id = SED.referenced_id
      INNER JOIN sys.objects AS O ON SED.referencing_id = O.object_id
)
INSERT INTO @udtt_dependencies_to_drop
  (
    referencing_schema_name,
    referencing_object_name,
    [type]
  )
  SELECT
      referencing_schema_name,
      referencing_object_name,
      [type]
    FROM
      udtt_dependencies_to_drop;

UPDATE @client_items
  SET
    drop_order =
      CASE
        WHEN ([type] = 'TT' /* AND needs_to_be_compiled = 'Y' */) THEN 1
        WHEN EXISTS
          (
            SELECT
                *
              FROM
                @udtt_dependencies_to_drop
              WHERE
                referencing_schema_name = [schema_name]
                AND referencing_object_name = [object_name]
          ) THEN 0
        ELSE 0
      END,
    needs_to_be_dropped =
      CASE
        WHEN ([type] = 'TT' AND needs_to_be_compiled = 'Y' AND is_present_on_server = 'Y') OR EXISTS
          (
            SELECT
                *
              FROM
                @udtt_dependencies_to_drop
              WHERE
                referencing_schema_name = [schema_name]
                AND referencing_object_name = [object_name]
          ) THEN 'Y'
        ELSE 'N'
      END;

/* SQL Server objects listed as dependencies of
   objects that need to be dropped,
   but don't exist in @client_items. */

WITH missing_client_files([schema_name], [object_name], [type])
AS
(
  SELECT referencing_schema_name, referencing_object_name, [type] FROM @udtt_dependencies_to_drop
  EXCEPT
  SELECT [schema_name], [object_name], [type] FROM @client_items
)
INSERT INTO @client_items
  (
    [schema_name],
    [object_name],
    [type],
    is_present_on_server,
    does_file_exist,
    needs_to_be_dropped
  )
  SELECT
      [schema_name],
      [object_name],
      [type],
      'Y',
      'N',
      'Y'
    FROM
      missing_client_files;

UPDATE @client_items
  SET
    needs_to_be_compiled =
      CASE
        WHEN needs_to_be_compiled = 'Y' OR needs_to_be_dropped = 'Y' OR is_present_on_server = 'N' THEN 'Y'
        ELSE 'N'
      END;

SELECT
    [pathname],
    [filename],
    [schema_name],
    [object_name],
    [type],
    needs_to_be_compiled,
    is_present_on_server,
    drop_order,
    does_file_exist,
    needs_to_be_dropped
  FROM
    @client_items;

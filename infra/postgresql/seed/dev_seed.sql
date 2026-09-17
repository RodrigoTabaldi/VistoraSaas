BEGIN;

INSERT INTO vistora.organizations ("Id", "Name", "CreatedAtUtc")
VALUES (
    '00000000-0000-0000-0000-000000000001',
    'Vistora Development',
    TIMESTAMPTZ '2026-01-01 00:00:00+00'
)
ON CONFLICT ("Id") DO NOTHING;

SELECT set_config('app.organization_id', '00000000-0000-0000-0000-000000000001', false);

INSERT INTO vistora.properties ("Id", organization_id, "Name", "Address", "CreatedAtUtc")
VALUES (
    '00000000-0000-0000-0000-000000000010',
    '00000000-0000-0000-0000-000000000001',
    'Residencial Aurora',
    'Rua das Flores, 10',
    TIMESTAMPTZ '2026-01-01 00:00:00+00'
)
ON CONFLICT ("Id") DO NOTHING;

INSERT INTO vistora.units ("Id", organization_id, "PropertyId", "Identifier")
VALUES (
    '00000000-0000-0000-0000-000000000011',
    '00000000-0000-0000-0000-000000000001',
    '00000000-0000-0000-0000-000000000010',
    'A-101'
)
ON CONFLICT ("Id") DO NOTHING;

INSERT INTO vistora.checklist_templates ("Id", organization_id, "Name", "IsActive", "CreatedAtUtc")
VALUES (
    '00000000-0000-0000-0000-000000000020',
    '00000000-0000-0000-0000-000000000001',
    'Checklist de entrada',
    TRUE,
    TIMESTAMPTZ '2026-01-01 00:00:00+00'
)
ON CONFLICT ("Id") DO NOTHING;

INSERT INTO vistora.inspections (
    "Id", organization_id, "UnitId", "ChecklistTemplateId", "Type", "RelatedInspectionId",
    "Status", "CreatedAtUtc", "CompletedAtUtc"
)
VALUES (
    '00000000-0000-0000-0000-000000000030',
    '00000000-0000-0000-0000-000000000001',
    '00000000-0000-0000-0000-000000000011',
    '00000000-0000-0000-0000-000000000020',
    'MoveIn',
    NULL,
    'Draft',
    TIMESTAMPTZ '2026-01-01 00:00:00+00',
    NULL
)
ON CONFLICT ("Id") DO NOTHING;

INSERT INTO vistora.inspection_rooms ("Id", organization_id, "InspectionId", "Name", "Position")
VALUES (
    '00000000-0000-0000-0000-000000000040',
    '00000000-0000-0000-0000-000000000001',
    '00000000-0000-0000-0000-000000000030',
    'Sala',
    1
)
ON CONFLICT ("Id") DO NOTHING;

INSERT INTO vistora.inspection_items (
    "Id", organization_id, "InspectionRoomId", "Description", "Response", "Notes", "Position"
)
VALUES (
    '00000000-0000-0000-0000-000000000050',
    '00000000-0000-0000-0000-000000000001',
    '00000000-0000-0000-0000-000000000040',
    'Paredes sem avarias',
    NULL,
    NULL,
    1
)
ON CONFLICT ("Id") DO NOTHING;

RESET app.organization_id;
COMMIT;
#!/usr/bin/env python3
"""Makes what the examples need and cannot be written by hand: invoice.xlsx (written with the standard library only, no Excel needed) and the zip of every sample file (zips/<file>.zip). An .xls (the old binary
format) cannot be written without a library; Edx reads one, and any Excel saves one (File > Save as > Excel 97-2003) from invoice.xlsx.

    python3 make_samples.py
"""
import os
import zipfile
from xml.sax.saxutils import escape

HERE = os.path.dirname(os.path.abspath(__file__))
SAMPLES = os.path.join(HERE, "..", "samples")
ZIPS = os.path.join(HERE, "..", "zips")

HEADER = ["invoiceNumber", "issueDate", "currency", "customerName", "customerTaxId", "customerTaxOffice", "customerDistrict", "customerCity", "customerCountry", "description", "quantity", "unitCode", "unitPrice", "vatRate"]
ROWS = [
    ["ABC2026000000001", "2026-10-28", "TRY", "Bilgera Bilisim A.S.", "1234567802", "Cankaya", "Cankaya", "Ankara", "Türkiye", "Danismanlik hizmeti", "2", "C62", "150.00", "20"],
    ["", "", "", "", "", "", "", "", "", "Yazilim lisansi", "1", "C62", "420.50", "20"],
]


def column(index):
    return chr(ord("A") + index)


def sheet_xml():
    rows = []
    for number, values in enumerate([HEADER, *ROWS], start=1):
        cells = "".join(f'<c r="{column(i)}{number}" t="inlineStr"><is><t>{escape(value)}</t></is></c>' for i, value in enumerate(values))
        rows.append(f'<row r="{number}">{cells}</row>')
    return f'<?xml version="1.0" encoding="UTF-8" standalone="yes"?><worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><sheetData>{"".join(rows)}</sheetData></worksheet>'


def write_xlsx(path):
    with zipfile.ZipFile(path, "w", zipfile.ZIP_DEFLATED) as book:
        book.writestr("[Content_Types].xml", '<?xml version="1.0" encoding="UTF-8"?><Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/><Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/><Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/></Types>')
        book.writestr("_rels/.rels", '<?xml version="1.0" encoding="UTF-8"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/></Relationships>')
        book.writestr("xl/workbook.xml", '<?xml version="1.0" encoding="UTF-8"?><workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"><sheets><sheet name="Invoice" sheetId="1" r:id="rId1"/></sheets></workbook>')
        book.writestr("xl/_rels/workbook.xml.rels", '<?xml version="1.0" encoding="UTF-8"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/></Relationships>')
        book.writestr("xl/worksheets/sheet1.xml", sheet_xml())


def main():
    write_xlsx(os.path.join(SAMPLES, "invoice.xlsx"))
    os.makedirs(ZIPS, exist_ok=True)
    for name in sorted(os.listdir(SAMPLES)):
        if name.endswith(".json"):
            continue
        with zipfile.ZipFile(os.path.join(ZIPS, name + ".zip"), "w", zipfile.ZIP_DEFLATED) as archive:
            archive.write(os.path.join(SAMPLES, name), name)
        print(f"zips/{name}.zip")


if __name__ == "__main__":
    main()

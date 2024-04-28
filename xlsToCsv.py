import pandas as pd
import argparse
import xlrd
import os


def failsafe_str(inp): 
    try:
        return str(inp)
    except:
        return inp   


def failsafe_int(inp): 
    try:
        return int(inp)
    except:
        return inp 

def createCsv(inf,outf,worksheet):
    try:
        if worksheet is None:
            final_file_name = outf
            read_file = pd.read_excel (inf)
        else :
            final_dir_name = os.path.dirname(inf)
            try:
                w_nm = int(worksheet)
                b_o_nm = os.path.basename(outf).split('.')[0]
                final_file_name = os.path.join(final_dir_name,b_o_nm+"_"+worksheet)+".csv"
            except:
                w_nm = worksheet
                final_file_name = os.path.join(final_dir_name,worksheet)+".csv"
            read_file = pd.read_excel (inf,sheet_name=w_nm)
        read_file = read_file.replace('\n','',regex=True)
        read_file.to_csv (final_file_name,index=False)
        return True
    except Exception as e:
        print(e)
        print("WorkSheet",worksheet,"Not Found")
        return False

 

def getcsv(input_file,output_file,worksheet_name=None):

    if worksheet_name is None:
        createCsv(input_file,output_file,None)
    else:
        ws_name = failsafe_str(worksheet_name)
        if ws_name.upper() == 'ALL':
            sheetname = xlrd.open_workbook(input_file)
            for sheet in sheetname.sheets():
                print(sheet.name)
                createCsv(input_file,output_file,sheet.name)
        else :
            w_s_name = ws_name.split(',')
            for nm in w_s_name:
                createCsv(input_file,output_file,nm)



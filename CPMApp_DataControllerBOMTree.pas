unit CPMApp_DataControllerBOMTree;

interface

uses Windows, Classes, SysUtils, DB, Dialogs, Variants,
  CPMApp_DB, CPMApp_DataObject, CPMApp_DataObjectDB,
  CPMApp_DataControllerBOMObject, CPMApp_DataControllerMaliyet,
  CPMApp_DataControllerCommon, CPMApp_DataControllerLotKapat,
  CPMApp_DocStatusWindow, CPMApp_BOMConsts,
  CPMApp_DocMatchFields,
  CPMApp_TableBOMSystem, CPMApp_TableStokSystem;

type

  TAppDataControllerBOMTree = class;

  TAppBOMData = class
    ID: Integer;
    ReceteNo: string;
    RevizyonNo: string;
    ReceteSiraNo: Integer;
    MalTip: TAppBOMType;
    MalKod: string;
    VersiyonNo: string;
    KullanimKod: string;
    SurumNo: Integer;
    DepoKod: string;
    SeriNo: string;
    SeriNo2: string;
    PozNo: string;
    OperasyonNo: Smallint;
    KaynakIslemNo: Smallint;
    Tarih: TDateTime;
    Miktar: Double;
    FireMiktar: Double;
    Birim: string;
    YanUrunNo: Smallint;
    YanUrunMaliyetOran: Double;
    KodUretim: Smallint;
    TeminTip: Smallint;
    TeminYontem: Smallint;
    SeviyeKod: Smallint;
    SonSeviye: Boolean;
    ReqType: TAppMRPReqType;
    MatchValues: array of variant;
    Maliyet: TAppMaliyetData;
    Parent: TAppBOMData;
  end;

  TAppBOMDataList = class
  private
    FList: TList;
  public
    constructor Create;
    destructor Destroy; override;
    procedure Clear;
    procedure Add(Data: TAppBOMData);
  published
    property List: TList read FList;
  end;

  TAppROMData = class
    ID: Integer;
    ReceteNo: String;
    RevizyonNo: String;
    OperasyonNo: Smallint;
    SonrakiOperasyonNo: String;
    RotaTip: TAppROMType;
    IsMerkezKod: String;
    KaynakKod: String;
    SiraNo: Smallint;
    Aciklama: String;
    CalismaSure: Double;
    KurulumSure: Double;
    SokumSure: Double;
    ToplamSure: Double;
    Kullanilan: Boolean;
    IslemTip: Smallint;
    OperatorKod: String;
    SeviyeKod: Smallint;
    OncekiIslemNo: String;
    OncekiIslemDurum: Integer;
    EylemTip: Integer;
    ParentID: Integer;
  end;

  TAppMamulKartData = class
    // Mamül Kart
    ReceteNo: String;
    RevizyonNo: String;
    ReceteSiraNo: Integer;
    HammaddeKod: String;
    HammaddeVersiyonNo: String;
    HammaddeSurumNo: Integer;
    HammaddeBirim: String;
    HayaletMontaj: Boolean;
    CoProduct: Boolean;
    MiktarTip: Smallint;
    Miktar: Double;
    PozNo: String;
    YanUrunNo: Smallint;
    YanUrunMiktar: Double;
    YanUrunMaliyetTip: Smallint;
    YanUrunMaliyetOran: Double;
    OperasyonNo: Smallint;
    OperasyonFireKullan: Smallint;
    OperasyonFireOran: Double;
    OperasyonFireMiktar: Double;
    BilesenFireOran: Double;
    KaynakIslemNo: Smallint;
    DepoKod: String;
    KodUretim: Smallint;

    // Mamül Stok Kart ve Stok Birim Alanları
    MamulStokKartBirim: String;
    MamulStokKartMontajFireOran: Double;
    MamulStokBirimKatsayi: Double;

    // Mamül Başlık
    MamulBirim: String;
    MamulDepoKod: String;
    MamulHammaddeDepoKod: String;

    // Hammadde Stok Kart, Stok Birim ve Mamül Başlık Alanları
    HammaddeStokKartBirim: String;
    HammaddeStokKartBilesenFireOran: Double;
    HammaddeStokKartYuvarlama: Smallint;
    HammaddeStokKartMaliyetGrupNo: Smallint;
    HammaddeStokKartTeminTip: Smallint;
    HammaddeStokKartTeminYontem: Smallint;
    HammaddeStokBirimKatsayi: Double;
  end;

  TAppDataControllerBOMTreeParams = class
  private
    FOwner: TAppDataControllerBOMTree;
    FMamulDepoKod: string;
    FHammaddeDepoKod: string;
    FReqType: TAppMRPReqType;

    FSingleLevel: Boolean;
    FReturnPhantom: Boolean;
    FReturnRoute: Boolean;
    FReturnProcess: Boolean;
    FCalcScrap: Smallint;
    FRound: Boolean;
    FLotKapat: Boolean;
    FCheckMRPTip: Boolean;
    FEvrakSiraNo: Integer;
    FEvrakTip: Smallint;
    FEvrakNo: String;
    FHesapKod: String;
    FOzelReceteTip: Smallint;
    FSipariseUretim: Boolean;
    FKullanimGrupNo: Integer;
    FMRPSiraNo: Integer;
    FTopluIslem: Boolean;
    procedure SetTopluIslem(const Value: Boolean);
  public
    constructor Create(AOwner: TAppDataControllerBOMTree);
    destructor Destroy; override;
  published
    // Genel Parametreler
    property SingleLevel: Boolean read FSingleLevel write FSingleLevel;
    property SipariseUretim: Boolean read FSipariseUretim write FSipariseUretim;
    property ReturnPhantom: Boolean read FReturnPhantom write FReturnPhantom;
    property ReturnRoute: Boolean read FReturnRoute write FReturnRoute;
    property ReturnProcess: Boolean read FReturnProcess write FReturnProcess;
    property CalcScrap: Smallint read FCalcScrap write FCalcScrap;
    property Round: Boolean read FRound write FRound;
    property LotKapat: Boolean read FLotKapat write FLotKapat;
    property KullanimGrupNo: Integer read FKullanimGrupNo write FKullanimGrupNo;
    // MRP için Parametreler
    property CheckMRPTip: Boolean read FCheckMRPTip write FCheckMRPTip;
    property ReqType: TAppMRPReqType read FReqType write FReqType;
    property MRPSiraNo: Integer read FMRPSiraNo write FMRPSiraNo;
    property TopluIslem: Boolean read FTopluIslem write SetTopluIslem;
    // Read Only Params
    property MamulDepoKod: string read FMamulDepoKod;
    property HammaddeDepoKod: string read FHammaddeDepoKod;
    property OzelReceteTip: Smallint read FOzelReceteTip;
    property EvrakTip: Smallint read FEvrakTip;
    property HesapKod: String read FHesapKod;
    property EvrakNo: String read FEvrakNo;
    property EvrakSiraNo: Integer read FEvrakSiraNo;
  end;

  TAppBOMTreeReturnEvent = procedure (BOMData: TAppBOMData) of object;
  TAppBOMTreeReturnRouteEvent = procedure (ROMData: TAppROMData) of object;
  TAppBOMTreeReturnErrorEvent = procedure (ErrClassName, ErrCode, ErrMessage: string; BOMData: TAppBOMData) of object;

  TAppDataControllerBOMTree = class
  private
    // Var
    FID: Integer;
    // Main Objects
    FMamulKart: TAppBOMChildMamulKart;
    FMamulKurulum: TAppBOMChildKaynakMamulKurulum;
    FOzelMamulKart: TAppBOMChildOzelMamulKart;
    FStokKart: TAppBOMChildStokKart;
    FKaynakIslem: TAppBOMChildKaynakIslemTanim;
    // Iternal Objects
    HammaddeData: TAppBOMData;
    RotaData: TAppROMData;
    KaynakData: TAppROMData;
    KaynakIslemData: TAppROMData;
    MamulKartData: TAppMamulKartData;
    FMemTableYanUrun: TTableMamulYanUrun;
    FMemTableRota: TTableMamulRota;
    FMemTableSonrakiOperasyon: TTableMamulRotaSonrakiOperasyon;
    FMemTableKaynak: TTableMamulRotaKaynak;
    // Childs
    FParams: TAppDataControllerBOMTreeParams;
    FStatus: TAppDocStatusWindow;
    // Data Controllers
    FdcCommon: TAppDataControllerCustomCommon;
    FdcMaliyet: TAppDataControllerMaliyet;
    FdcLotKapat: TAppDataControllerCustomLotKapat;
    // Results
    FOnReturn: TAppBOMTreeReturnEvent;
    FOnReturnRoute: TAppBOMTreeReturnRouteEvent;
    FOnReturnError: TAppBOMTreeReturnErrorEvent;
    FTableOzelMamulKart: TTableOzelMamulKart;
    FMatchFields: TAppRuleMatchFields;
    // Functions
    function NewID: Integer;
    function InternalExpand(MamulData: TAppBOMData): TAppMaliyetData;
    procedure InternalWhereUsed(Data: TAppBOMData);
    // Procedures
  protected
    procedure DoOnReturn(BOMData: TAppBOMData); virtual;
    procedure DoOnReturnRoute(ROMData: TAppROMData); virtual;
    procedure DoOnReturnError(ErrClassName, ErrCode, ErrMessage: string; BOMData: TAppBOMData); virtual;
  public
    constructor Create; virtual;
    destructor Destroy; override;
    procedure Start; virtual;
    procedure Finish; virtual;
    procedure Expand(MamulData: TAppBOMData); reintroduce; overload;
    procedure Expand(MamulKod, VersiyonNo, KullanimKod: string; SurumNo: Integer; Tarih: TDateTime; Miktar: Double; Birim, MamulDepoKod, HammaddeDepoKod: string; OzelReceteTip, EvrakTip: Smallint; HesapKod, EvrakNo: String; EvrakSiraNo: Integer); reintroduce; overload;
    procedure Expand(MamulKod, VersiyonNo, KullanimKod: string; SurumNo: Integer; Tarih: TDateTime; Miktar: Double; Birim, MamulDepoKod, HammaddeDepoKod: string); reintroduce; overload;
    procedure Expand(StokKartFilterText: string; StokKartFieldList: TStrings); reintroduce; overload;
    procedure WhereUsed(MamulKod, VersiyonNo, KullanimKod: string; SurumNo: Integer; Tarih: TDateTime; Miktar: Double; Birim: string);
  published
    // Data Controllers
    property dcCommon: TAppDataControllerCustomCommon read FdcCommon;
    property dcMaliyet: TAppDataControllerMaliyet read FdcMaliyet;
    property dcLotKapat: TAppDataControllerCustomLotKapat read FdcLotKapat;
    // Objects
    property MamulKart: TAppBOMChildMamulKart read FMamulKart;
    property MamulKurulum: TAppBOMChildKaynakMamulKurulum read FMamulKurulum;
    property OzelMamulKart: TAppBOMChildOzelMamulKart read FOzelMamulKart;
    property StokKart: TAppBOMChildStokKart read FStokKart;
    property KaynakIslem: TAppBOMChildKaynakIslemTanim read FKaynakIslem;
    // Childs
    property Params: TAppDataControllerBOMTreeParams read FParams;
    property TableOzelMamulKart: TTableOzelMamulKart read FTableOzelMamulKart write FTableOzelMamulKart;
    property MatchFields: TAppRuleMatchFields read FMatchFields;
    // Returns
    property OnReturn: TAppBOMTreeReturnEvent read FOnReturn write FOnReturn;
    property OnReturnRoute: TAppBOMTreeReturnRouteEvent read FOnReturnRoute write FOnReturnRoute;
    property OnReturnError: TAppBOMTreeReturnErrorEvent read FOnReturnError write FOnReturnError;
  end;

const
  // Error Codes
  ErrCode_MamulKartNotFound: String = 'ER001';
  ErrCode_StokBirimCevrimNotFound: String = 'ER002';
  ErrCode_DepoKartNotFound: String = 'ER003';
  ErrCode_MamulAgacUnknownError: String = 'ER999';

  // Exception Codes

implementation

uses CPMApp_Math, CPMApp_Dialogs, CPMApp_Date, CPMApp_DBUtils;

procedure ResetMaliyetData(var Res: TAppMaliyetData);
begin
  Res.BirimMaliyet1 := 0;
  Res.BirimMaliyet1DovizCins := '';
  Res.BirimMaliyet1DovizKur := 0;
  Res.BirimMaliyet2 := 0;
  Res.BirimMaliyet3 := 0;
  Res.BirimMaliyet := 0;
  Res.Maliyet := 0;
  Res.MaliyetGrup1 := 0;
  Res.MaliyetGrup2 := 0;
  Res.MaliyetGrup3 := 0;
  Res.MaliyetGrup4 := 0;
  Res.MaliyetGrupDiger := 0;
  Res.YerelBirimMaliyet1 := 0;
  Res.YerelBirimMaliyet2 := 0;
  Res.YerelBirimMaliyet3 := 0;
  Res.YerelBirimMaliyet := 0;
  Res.YerelMaliyet1 := 0;
  Res.YerelMaliyet2 := 0;
  Res.YerelMaliyet3 := 0;
  Res.YerelMaliyet := 0;
  Res.YerelMaliyetGrup1 := 0;
  Res.YerelMaliyetGrup2 := 0;
  Res.YerelMaliyetGrup3 := 0;
  Res.YerelMaliyetGrup4 := 0;
  Res.YerelMaliyetGrupDiger := 0;
end;

procedure IncMaliyetData(ChildRes: TAppMaliyetData; var MainRes: TAppMaliyetData);
begin
  MainRes.Maliyet := MainRes.Maliyet + ChildRes.Maliyet;
  MainRes.MaliyetGrup1 := MainRes.MaliyetGrup1 + ChildRes.MaliyetGrup1;
  MainRes.MaliyetGrup2 := MainRes.MaliyetGrup2 + ChildRes.MaliyetGrup2;
  MainRes.MaliyetGrup3 := MainRes.MaliyetGrup3 + ChildRes.MaliyetGrup3;
  MainRes.MaliyetGrup4 := MainRes.MaliyetGrup4 + ChildRes.MaliyetGrup4;
  MainRes.MaliyetGrupDiger := MainRes.MaliyetGrupDiger + ChildRes.MaliyetGrupDiger;
  MainRes.YerelMaliyet1 := MainRes.YerelMaliyet1 + ChildRes.YerelMaliyet1;
  MainRes.YerelMaliyet2 := MainRes.YerelMaliyet2 + ChildRes.YerelMaliyet2;
  MainRes.YerelMaliyet3 := MainRes.YerelMaliyet3 + ChildRes.YerelMaliyet3;
  MainRes.YerelMaliyet := MainRes.YerelMaliyet + ChildRes.YerelMaliyet;
  MainRes.YerelMaliyetGrup1 := MainRes.YerelMaliyetGrup1 + ChildRes.YerelMaliyetGrup1;
  MainRes.YerelMaliyetGrup2 := MainRes.YerelMaliyetGrup2 + ChildRes.YerelMaliyetGrup2;
  MainRes.YerelMaliyetGrup3 := MainRes.YerelMaliyetGrup3 + ChildRes.YerelMaliyetGrup3;
  MainRes.YerelMaliyetGrup4 := MainRes.YerelMaliyetGrup4 + ChildRes.YerelMaliyetGrup4;
  MainRes.YerelMaliyetGrupDiger := MainRes.YerelMaliyetGrupDiger + ChildRes.YerelMaliyetGrupDiger;
end;

procedure DecMaliyetData(ChildRes: TAppMaliyetData; var MainRes: TAppMaliyetData);
begin
  MainRes.Maliyet := MainRes.Maliyet - ChildRes.Maliyet;
  MainRes.MaliyetGrup1 := MainRes.MaliyetGrup1 - ChildRes.MaliyetGrup1;
  MainRes.MaliyetGrup2 := MainRes.MaliyetGrup2 - ChildRes.MaliyetGrup2;
  MainRes.MaliyetGrup3 := MainRes.MaliyetGrup3 - ChildRes.MaliyetGrup3;
  MainRes.MaliyetGrup4 := MainRes.MaliyetGrup4 - ChildRes.MaliyetGrup4;
  MainRes.MaliyetGrupDiger := MainRes.MaliyetGrupDiger - ChildRes.MaliyetGrupDiger;
  MainRes.YerelMaliyet1 := MainRes.YerelMaliyet1 - ChildRes.YerelMaliyet1;
  MainRes.YerelMaliyet2 := MainRes.YerelMaliyet2 - ChildRes.YerelMaliyet2;
  MainRes.YerelMaliyet3 := MainRes.YerelMaliyet3 - ChildRes.YerelMaliyet3;
  MainRes.YerelMaliyet := MainRes.YerelMaliyet - ChildRes.YerelMaliyet;
  MainRes.YerelMaliyetGrup1 := MainRes.YerelMaliyetGrup1 - ChildRes.YerelMaliyetGrup1;
  MainRes.YerelMaliyetGrup2 := MainRes.YerelMaliyetGrup2 - ChildRes.YerelMaliyetGrup2;
  MainRes.YerelMaliyetGrup3 := MainRes.YerelMaliyetGrup3 - ChildRes.YerelMaliyetGrup3;
  MainRes.YerelMaliyetGrup4 := MainRes.YerelMaliyetGrup4 - ChildRes.YerelMaliyetGrup4;
  MainRes.YerelMaliyetGrupDiger := MainRes.YerelMaliyetGrupDiger - ChildRes.YerelMaliyetGrupDiger;
end;

procedure EndMaliyetData(AMiktar: Double; var Res: TAppMaliyetData);
begin
  Res.BirimMaliyet1 := AppDiv(Res.YerelMaliyet1, AMiktar);
  Res.BirimMaliyet1DovizCins := '';
  Res.BirimMaliyet1DovizKur := 0;
  Res.BirimMaliyet2 := AppDiv(Res.YerelMaliyet2, AMiktar);
  Res.BirimMaliyet3 := AppDiv(Res.YerelMaliyet3, AMiktar);
  Res.BirimMaliyet := AppDiv(Res.YerelMaliyet, AMiktar);

  Res.YerelBirimMaliyet1 := AppDiv(Res.YerelMaliyet1, AMiktar);
  Res.YerelBirimMaliyet2 := AppDiv(Res.YerelMaliyet2, AMiktar);
  Res.YerelBirimMaliyet3 := AppDiv(Res.YerelMaliyet3, AMiktar);
  Res.YerelBirimMaliyet := AppDiv(Res.YerelMaliyet, AMiktar);
end;

{ TAppBOMDataList }

procedure TAppBOMDataList.Add(Data: TAppBOMData);
var
  AData: TAppBOMData;
  I: Integer;
begin
  AData := TAppBOMData.Create;

  AData.ID := Data.ID;

  AData.ReceteNo := Data.ReceteNo;
  AData.RevizyonNo := Data.RevizyonNo;
  AData.ReceteSiraNo := Data.ReceteSiraNo;

  AData.MalTip := Data.MalTip;
  AData.MalKod := Data.MalKod;
  AData.VersiyonNo := Data.VersiyonNo;
  AData.SurumNo := Data.SurumNo;
  AData.DepoKod := Data.DepoKod;
  AData.SeriNo := Data.SeriNo;
  AData.SeriNo2 := Data.SeriNo2;
  AData.PozNo := Data.PozNo;
  AData.OperasyonNo := Data.OperasyonNo;
  AData.KaynakIslemNo := Data.KaynakIslemNo;

  AData.Tarih := Data.Tarih;
  AData.Miktar := Data.Miktar;
  AData.FireMiktar := Data.FireMiktar;
  AData.Birim := Data.Birim;

  AData.YanUrunNo := Data.YanUrunNo;
  AData.YanUrunMaliyetOran := Data.YanUrunMaliyetOran;

  AData.KodUretim := Data.KodUretim;
  AData.TeminTip := Data.TeminTip;
  AData.TeminYontem := Data.TeminYontem;
  AData.SeviyeKod := Data.SeviyeKod;
  AData.SonSeviye := Data.SonSeviye;
  AData.ReqType := Data.ReqType;

  SetLength(AData.MatchValues, Length(Data.MatchValues));
  for I := 0 to High(Data.MatchValues) do
    AData.MatchValues[I] := Data.MatchValues[I];

  AData.Maliyet := Data.Maliyet;

  AData.Parent := Data.Parent;

  FList.Add(AData);
end;

procedure TAppBOMDataList.Clear;
var
  I: Integer;
begin
  for I := FList.Count - 1 downto 0 do
    TAppBOMData(FList[I]).Free;
  FList.Clear;
end;

constructor TAppBOMDataList.Create;
begin
  inherited Create;
  FList := TList.Create;
end;

destructor TAppBOMDataList.Destroy;
begin
  Clear;
  FreeAndNil(FList);
  inherited;
end;

{ TAppDataControllerBOMTreeParams }

constructor TAppDataControllerBOMTreeParams.Create(AOwner: TAppDataControllerBOMTree);
begin
  FOwner := AOwner;
  
                         // Ağaç        Üretim       MRP     Açıklama
  SingleLevel := True;   // Parametrik  True         True    Genel
  SipariseUretim := False;
  ReturnPhantom := False;// Parametrik  True         True    Ağaç için eklendi
  ReturnRoute := False;  // Parametrik  True         False   Genel
  ReturnProcess := False;// Parametrik  True         False   Genel
  CalcScrap := 1;        // Parametrik  Parametrik   1       Genel
  Round := True;         // Parametrik  Parametrik   True    Genel
  LotKapat := False;     // Parametrik  True         False   Üretim için eklendi.

  FReqType := mrprtBagimli;

  // Stok Kapat kullanıldığında verilmek zorunda.
  FMamulDepoKod := '';
  FHammaddeDepoKod := '';

  // Özel Reçete varsa verilmek zorunda
  FOzelReceteTip := 0;
  FEvrakTip := 0;
  FHesapKod := '';
  FEvrakNo := '';
  FEvrakSiraNo := 0;
end;

destructor TAppDataControllerBOMTreeParams.Destroy;
begin

  inherited;
end;

procedure TAppDataControllerBOMTreeParams.SetTopluIslem(const Value: Boolean);
begin
  FTopluIslem := Value;
  if Assigned(FOwner.MamulKart) then
    FOwner.MamulKart.TopluIslem := Value;
end;

{ TAppDataControllerBOMTree }

constructor TAppDataControllerBOMTree.Create;
begin
  // Objects
  HammaddeData := TAppBOMData.Create;
  RotaData := TAppROMData.Create;
  KaynakData := TAppROMData.Create;
  KaynakIslemData := TAppROMData.Create;
  MamulKartData := TAppMamulKartData.Create;
  FParams := TAppDataControllerBOMTreeParams.Create(Self);
  FStatus := TAppDocStatusWindow.Create;

  FMamulKart := TAppBOMChildMamulKart.Create(nil);
  FMamulKurulum := TAppBOMChildKaynakMamulKurulum.Create(nil);
  FOzelMamulKart := TAppBOMChildOzelMamulKart.Create(nil);
  FStokKart := TAppBOMChildStokKart.Create(nil);
  FKaynakIslem := TAppBOMChildKaynakIslemTanim.Create(nil);

  // Data Controllers
  FdcCommon := TAppDataControllerCustomCommon.Create;
  FdcMaliyet := TAppDataControllerMaliyet.Create(FdcCommon);
  FdcLotKapat := TAppDataControllerCustomLotKapat.Create(FdcCommon);

  FMemTableYanUrun := TTableMamulYanUrun.Create(nil);
  FMemTableRota := TTableMamulRota.Create(nil);
  FMemTableSonrakiOperasyon := TTableMamulRotaSonrakiOperasyon.Create(nil);
  FMemTableKaynak := TTableMamulRotaKaynak.Create(nil);

  FMatchFields := TAppRuleMatchFields.Create;
  FMatchFields.Section := 'BOMTREEMATCHFIELDS';
  FMatchFields.Ident := 'MAMULKART';
  FMatchFields.TableSrc := FMamulKart.TableKart;
end;

destructor TAppDataControllerBOMTree.Destroy;
begin
  // Childs
  FreeAndNil(HammaddeData);
  FreeAndNil(RotaData);
  FreeAndNil(KaynakData);
  FreeAndNil(KaynakIslemData);
  FreeAndNil(MamulKartData);
  FreeAndNil(FParams);
  FreeAndNil(FStatus);

  FreeAndNil(FMamulKart);
  FreeAndNil(FMamulKurulum);
  FreeAndNil(FOzelMamulKart);
  FreeAndNil(FStokKart);
  FreeAndNil(FKaynakIslem);

  FreeAndNil(FMemTableYanUrun);
  FreeAndNil(FMemTableRota);
  FreeAndNil(FMemTableSonrakiOperasyon);
  FreeAndNil(FMemTableKaynak);

  FreeAndNil(FMatchFields);

  // Data Controllers
  FreeAndNil(FdcLotKapat);
  FreeAndNil(FdcMaliyet);
  FreeAndNil(FdcCommon);

  inherited;
end;

procedure TAppDataControllerBOMTree.DoOnReturn(BOMData: TAppBOMData);
begin
  if Assigned(FOnReturn) then
    FOnReturn(BOMData);
end;

procedure TAppDataControllerBOMTree.DoOnReturnRoute(ROMData: TAppROMData);
begin
  if Assigned(FOnReturnRoute) then
    FOnReturnRoute(ROMData);
end;

procedure TAppDataControllerBOMTree.DoOnReturnError(ErrClassName, ErrCode,
  ErrMessage: string; BOMData: TAppBOMData);
begin
  if Assigned(FOnReturnError) then
    FOnReturnError(ErrClassName, ErrCode, ErrMessage, BOMData);
end;

function TAppDataControllerBOMTree.NewID: Integer;
begin
  FID := FID + 1;
  Result := FID;
end;

procedure TAppDataControllerBOMTree.Start;
begin
  FID := 0;
  dcMaliyet.Start;
end;

procedure TAppDataControllerBOMTree.WhereUsed(MamulKod, VersiyonNo,
  KullanimKod: string; SurumNo: Integer; Tarih: TDateTime; Miktar: Double;
  Birim: string);
var
  AData: TAppBOMData;
begin
  AData := TAppBOMData.Create;
  try
    AData.ID := NewID;

    AData.MalTip := botProduct;
    AData.MalKod := MamulKod;
    AData.VersiyonNo := VersiyonNo;
    AData.KullanimKod := KullanimKod;
    AData.SurumNo := SurumNo;

    AData.Tarih := Tarih;
    AData.Miktar := Miktar;
    AData.Birim := Birim;

    AData.SeviyeKod := 0; // 0 - Kendisi 1- Birinci Seviye ve 2 den sonra diğer seviyeler

    InternalWhereUsed(AData);
  finally
    FreeAndNil(AData);
  end;
end;

function TAppDataControllerBOMTree.InternalExpand(MamulData: TAppBOMData): TAppMaliyetData;
var
  ChildMamulList: TAppBOMDataList;
  vOzelMamulKart: TTableOzelMamulKart;

  procedure CheckMemTables;
  begin
    if not FMemTableYanUrun.Active then
    begin
      FMemTableYanUrun.FieldDefs.Assign(FMamulKart.TableYanUrun.FieldDefs);
      FMemTableYanUrun.CreateFields;
      FMemTableYanUrun.CreateDataSet;
      FMemTableYanUrun.IndexFieldNames := 'RECETENO;REVIZYONNO;YANURUNNO';
    end;

    if not FMemTableRota.Active then
    begin
      FMemTableRota.FieldDefs.Assign(FMamulKart.TableRota.FieldDefs);
      FMemTableRota.CreateFields;
      FMemTableRota.CreateDataSet;
      FMemTableRota.IndexFieldNames := 'RECETENO;REVIZYONNO;OPERASYONNO';
    end;

    if not FMemTableSonrakiOperasyon.Active then
    begin
      FMemTableSonrakiOperasyon.FieldDefs.Assign(FMamulKart.TableRotaSonrakiOperasyon.FieldDefs);
      FMemTableSonrakiOperasyon.CreateFields;
      FMemTableSonrakiOperasyon.CreateDataSet;
      FMemTableSonrakiOperasyon.IndexFieldNames := 'RECETENO;REVIZYONNO;OPERASYONNO;SONRAKIOPERASYONNO';
    end;

    if not FMemTableKaynak.Active then
    begin
      FMemTableKaynak.FieldDefs.Assign(FMamulKart.TableRotaKaynak.FieldDefs);
      FMemTableKaynak.CreateFields;
      FMemTableKaynak.CreateDataSet;
      FMemTableKaynak.IndexFieldNames := 'RECETENO;REVIZYONNO;OPERASYONNO;KULLANIMSIRANO';
    end;
  end;

  function IsSkipped(AMalTip: TAppBOMType): Boolean;
  begin
    if Params.ReturnPhantom then
      Result := False
    else
      Result := AMalTip = botPhantomAssembly;
  end;

  function IsValidItem(ABasTarih, ABitTarih: TDateTime; AMRPTip: Smallint): Boolean;
  begin
    Result := (ABasTarih <= MamulData.Tarih) and ((ABitTarih = AppFirstDate) or (ABitTarih >= MamulData.Tarih));
    if Result and Params.CheckMRPTip then
      Result := AMRPTip > 0;
  end;

  procedure FetchMamulKart;
  begin
    MamulKartData.ReceteNo := FMamulKart.TableKart.ReceteNo;
    MamulKartData.RevizyonNo := FMamulKart.TableKart.RevizyonNo;
    MamulKartData.ReceteSiraNo := FMamulKart.TableKart.SiraNo;
    MamulKartData.CoProduct := False;
    MamulKartData.HammaddeKod := FMamulKart.TableKart.HammaddeKod;
    MamulKartData.HammaddeVersiyonNo := FMamulKart.TableKart.HammaddeVersiyonNo;
    MamulKartData.HammaddeSurumNo := FMamulKart.TableKart.HammaddeSurumNo;
    MamulKartData.HammaddeBirim := FMamulKart.TableKart.HammaddeBirim;
    MamulKartData.HayaletMontaj := FMamulKart.TableKart.HammaddeKullanimSekli = 4;
    MamulKartData.MiktarTip := FMamulKart.TableKart.MiktarTip;
    MamulKartData.Miktar := FMamulKart.TableKart.Miktar;
    MamulKartData.PozNo := FMamulKart.TableKart.PozNo;
    MamulKartData.YanUrunNo := FMamulKart.TableKart.YanUrunNo;
    MamulKartData.YanUrunMiktar := FMamulKart.TableKart.YanUrunMiktar;
    MamulKartData.YanUrunMaliyetTip := FMamulKart.TableKart.YanUrunMaliyetTip;
    MamulKartData.YanUrunMaliyetOran := FMamulKart.TableKart.YanUrunMaliyetOran;
    MamulKartData.OperasyonNo := FMamulKart.TableKart.OperasyonNo;
    MamulKartData.OperasyonFireKullan := FMamulKart.TableKart.OperasyonFireKullan;
    MamulKartData.OperasyonFireOran := FMamulKart.TableKart.OperasyonFireOran;
    MamulKartData.OperasyonFireMiktar := FMamulKart.TableKart.OperasyonFireMiktar;
    MamulKartData.BilesenFireOran := FMamulKart.TableKart.BilesenFireOran;
    MamulKartData.KaynakIslemNo := FMamulKart.TableKart.KaynakIslemNo;
    MamulKartData.DepoKod := FMamulKart.TableKart.DepoKod;
    MamulKartData.KodUretim := FMamulKart.TableKart.KodUretim;

    MamulKartData.MamulStokKartBirim := FMamulKart.TableKart.MamulStokKartBirim;
    MamulKartData.MamulStokKartMontajFireOran := FMamulKart.TableKart.MamulStokKartMontajFireOran;

    MamulKartData.MamulStokBirimKatsayi := FMamulKart.TableKart.MamulStokBirimKatsayi;

    MamulKartData.MamulBirim := FMamulKart.TableKart.MamulBirim;
    MamulKartData.MamulDepoKod := FMamulKart.TableKart.MamulDepoKod;
    MamulKartData.MamulHammaddeDepoKod := FMamulKart.TableKart.MamulHammaddeDepoKod;

    MamulKartData.HammaddeStokKartBirim := FMamulKart.TableKart.HammaddeStokKartBirim;
    MamulKartData.HammaddeStokKartTeminTip := FMamulKart.TableKart.HammaddeStokKartTeminTip;
    MamulKartData.HammaddeStokKartTeminYontem := FMamulKart.TableKart.HammaddeStokKartTeminYontem;
    MamulKartData.HammaddeStokKartBilesenFireOran := FMamulKart.TableKart.HammaddeStokKartBilesenFireOran;
    MamulKartData.HammaddeStokKartYuvarlama := FMamulKart.TableKart.HammaddeStokKartYuvarlama;
    MamulKartData.HammaddeStokKartMaliyetGrupNo := FMamulKart.TableKart.HammaddeStokKartMaliyetGrupNo;

    MamulKartData.HammaddeStokBirimKatsayi := FMamulKart.TableKart.HammaddeStokBirimKatsayi;
  end;

  procedure FetchOzelMamulKart;
  begin
    MamulKartData.ReceteNo := '';
    MamulKartData.RevizyonNo := '';
    MamulKartData.ReceteSiraNo := 0;
    MamulKartData.CoProduct := False;
    MamulKartData.HammaddeKod := vOzelMamulKart.HammaddeKod;
    MamulKartData.HammaddeVersiyonNo := vOzelMamulKart.HammaddeVersiyonNo;
    MamulKartData.HammaddeSurumNo := vOzelMamulKart.HammaddeSurumNo;
    MamulKartData.HammaddeBirim := vOzelMamulKart.HammaddeBirim;
    MamulKartData.HayaletMontaj := False;
    MamulKartData.MiktarTip := 0; // Oransal
    MamulKartData.Miktar := vOzelMamulKart.HammaddeBirimMiktar;
    MamulKartData.PozNo := vOzelMamulKart.PozNo;
    MamulKartData.YanUrunNo := 0;
    MamulKartData.YanUrunMiktar := 0;
    MamulKartData.YanUrunMaliyetTip := 0;
    MamulKartData.YanUrunMaliyetOran := 0;
    MamulKartData.OperasyonNo := vOzelMamulKart.OperasyonNo;
    MamulKartData.OperasyonFireKullan := 0;
    MamulKartData.OperasyonFireOran := 0;
    MamulKartData.OperasyonFireMiktar := 0;
    MamulKartData.BilesenFireOran := 0;
    MamulKartData.KaynakIslemNo := 0;
    MamulKartData.DepoKod := vOzelMamulKart.DepoKod;
    MamulKartData.KodUretim := 1;

    MamulKartData.MamulStokKartBirim := vOzelMamulKart.MamulStokKartBirim;
    MamulKartData.MamulStokKartMontajFireOran := vOzelMamulKart.MamulStokKartMontajFireOran;

    if dcCommon.StokBirim.Find(MamulData.MalKod, MamulKartData.MamulStokKartBirim) then
      MamulKartData.MamulStokBirimKatsayi := dcCommon.StokBirim.Table.Katsayi
    else
      MamulKartData.MamulStokBirimKatsayi := -1;

    MamulKartData.MamulBirim := vOzelMamulKart.MamulBirim;
    MamulKartData.MamulDepoKod := '';
    MamulKartData.MamulHammaddeDepoKod := '';

    MamulKartData.HammaddeStokKartBirim := vOzelMamulKart.HammaddeStokKartBirim;
    MamulKartData.HammaddeStokKartTeminTip := vOzelMamulKart.HammaddeStokKartTeminTip;
    MamulKartData.HammaddeStokKartTeminYontem := vOzelMamulKart.HammaddeStokKartTeminYontem;
    MamulKartData.HammaddeStokKartBilesenFireOran := vOzelMamulKart.HammaddeStokKartBilesenFireOran;
    MamulKartData.HammaddeStokKartYuvarlama := vOzelMamulKart.HammaddeStokKartYuvarlama;
    MamulKartData.HammaddeStokKartMaliyetGrupNo := vOzelMamulKart.HammaddeStokKartMaliyetGrupNo;

    if dcCommon.StokBirim.Find(MamulData.MalKod, MamulKartData.MamulStokKartBirim) then
      MamulKartData.HammaddeStokBirimKatsayi := dcCommon.StokBirim.Table.Katsayi
    else
      MamulKartData.HammaddeStokBirimKatsayi := -1;
  end;

  procedure FetchMamulYanUrun;
  begin
    MamulKartData.ReceteNo := FMemTableYanUrun.ReceteNo;
    MamulKartData.RevizyonNo := FMemTableYanUrun.RevizyonNo;
    MamulKartData.CoProduct := True;
    MamulKartData.ReceteSiraNo := 0;
    MamulKartData.HammaddeKod := FMemTableYanUrun.MalKod;
    MamulKartData.HammaddeVersiyonNo := FMemTableYanUrun.VersiyonNo;
    MamulKartData.HammaddeSurumNo := 0;
    MamulKartData.HammaddeBirim := FMemTableYanUrun.Birim;
    MamulKartData.HayaletMontaj := False;
    MamulKartData.MiktarTip := 0;
    MamulKartData.Miktar := FMemTableYanUrun.Miktar;
    MamulKartData.PozNo := '';
    MamulKartData.YanUrunNo := FMemTableYanUrun.YanUrunNo;
    MamulKartData.YanUrunMiktar := 0;
    MamulKartData.YanUrunMaliyetTip := FMemTableYanUrun.MaliyetTip;
    MamulKartData.YanUrunMaliyetOran := FMemTableYanUrun.MaliyetOran;
    MamulKartData.OperasyonNo := FMemTableYanUrun.OperasyonNo;
    MamulKartData.OperasyonFireKullan := 0;
    MamulKartData.OperasyonFireOran := 0;
    MamulKartData.OperasyonFireMiktar := 0;
    MamulKartData.BilesenFireOran := 0;
    MamulKartData.KaynakIslemNo := 0;
    MamulKartData.DepoKod := FMemTableYanUrun.DepoKod;
    MamulKartData.KodUretim := 1;

    MamulKartData.MamulStokKartBirim := FMemTableYanUrun.MamulStokKartBirim;
    MamulKartData.MamulStokKartMontajFireOran := FMemTableYanUrun.MamulStokKartMontajFireOran;

    MamulKartData.MamulStokBirimKatsayi := FMemTableYanUrun.MamulStokBirimKatsayi;

    MamulKartData.MamulBirim := FMemTableYanUrun.MamulBirim;
    MamulKartData.MamulDepoKod := FMemTableYanUrun.MamulDepoKod;
    MamulKartData.MamulHammaddeDepoKod := '';

    MamulKartData.HammaddeStokKartBirim := FMemTableYanUrun.HammaddeStokKartBirim;
    MamulKartData.HammaddeStokKartTeminTip := FMemTableYanUrun.HammaddeStokKartTeminTip;
    MamulKartData.HammaddeStokKartTeminYontem := FMemTableYanUrun.HammaddeStokKartTeminYontem;
    MamulKartData.HammaddeStokKartBilesenFireOran := FMemTableYanUrun.HammaddeStokKartBilesenFireOran;
    MamulKartData.HammaddeStokKartYuvarlama := FMemTableYanUrun.HammaddeStokKartYuvarlama;
    MamulKartData.HammaddeStokKartMaliyetGrupNo := FMemTableYanUrun.HammaddeStokKartMaliyetGrupNo;

    MamulKartData.HammaddeStokBirimKatsayi := FMemTableYanUrun.HammaddeStokBirimKatsayi;
  end;

  procedure FetchMemYanUrun(AMamulData: TAppBOMData);
  begin
    FMamulKart.TableYanUrun.First;
    while not FMamulKart.TableYanUrun.Eof do
    begin
      FMemTableYanUrun.Append;
      //for I := 0 to FMamulKart.TableYanUrun.FieldCount - 1 do
      //  FMemTableYanUrun.Fields[I].Value := FMamulKart.TableYanUrun.Fields[I].Value;
      FMemTableYanUrun.FetchRecord(FMamulKart.TableYanUrun);
      if IsSkipped(AMamulData.MalTip) then
      begin
        FMemTableYanUrun.ReceteNo := AMamulData.Parent.ReceteNo;
        FMemTableYanUrun.RevizyonNo := AMamulData.Parent.RevizyonNo;
      end;
      FMemTableYanUrun.Post;

      FMamulKart.TableYanUrun.Next;
    end;
  end;

  procedure FetchMemRota(AMamulData: TAppBOMData);
  begin
    FMamulKart.TableRota.First;
    while not FMamulKart.TableRota.Eof do
    begin
      FMemTableRota.Append;
      //for I := 0 to FMamulKart.TableRota.FieldCount - 1 do
      //  FMemTableRota.Fields[I].Value := FMamulKart.TableRota.Fields[I].Value;
      FMemTableRota.FetchRecord(FMamulKart.TableRota);
      if IsSkipped(AMamulData.MalTip) then
      begin
        FMemTableRota.ReceteNo := AMamulData.Parent.ReceteNo;
        FMemTableRota.RevizyonNo := AMamulData.Parent.RevizyonNo;
      end;
      FMemTableRota.Post;

      FMamulKart.TableRota.Next;
    end;
  end;

  procedure FetchMemSonrakiOperasyon(AMamulData: TAppBOMData);
  begin
    FMamulKart.TableRotaSonrakiOperasyon.First;
    while not FMamulKart.TableRotaSonrakiOperasyon.Eof do
    begin
      FMemTableSonrakiOperasyon.Append;
      //for I := 0 to FMamulKart.TableRotaSonrakiOperasyon.FieldCount - 1 do
      //  FMemTableSonrakiOperasyon.Fields[I].Value := FMamulKart.TableRotaSonrakiOperasyon.Fields[I].Value;
      FMemTableSonrakiOperasyon.FetchRecord(FMamulKart.TableRotaSonrakiOperasyon);
      if IsSkipped(AMamulData.MalTip) then
      begin
        FMemTableSonrakiOperasyon.ReceteNo := AMamulData.Parent.ReceteNo;
        FMemTableSonrakiOperasyon.RevizyonNo := AMamulData.Parent.RevizyonNo;
      end;
      FMemTableSonrakiOperasyon.Post;

      FMamulKart.TableRotaSonrakiOperasyon.Next;
    end;
  end;

  procedure FetchMemKaynak(AMamulData: TAppBOMData);
  var
    ACalismaSure, AToplamSure: Double;
  begin
    FMamulKart.TableRotaKaynak.First;
    while not FMamulKart.TableRotaKaynak.Eof do
    begin
      if FMamulKart.TableRotaKaynak.KullanimTip = 1 then // Sadece kullanılım tip = 1 olanları ekliyoruz. 30.12.2013 Veysel
      begin
        if FMamulKurulum.Find(FMamulKart.TableRotaKaynak.KaynakKod, FMamulKart.TableRotaKaynak.MamulKod) then
          ACalismaSure := FMamulKurulum.Table.CalismaSure
        else
          ACalismaSure := FMamulKart.TableRotaKaynak.KaynakCalismaSure;
        AToplamSure := FMamulKart.TableRotaKaynak.KaynakKurulumSure + (AMamulData.Miktar * ACalismaSure) + FMamulKart.TableRotaKaynak.KaynakSokumSure;

        FMemTableKaynak.Append;
        //for I := 0 to FMamulKart.TableRotaKaynak.FieldCount - 1 do
        //  FMemTableKaynak.Fields[I].Value := FMamulKart.TableRotaKaynak.Fields[I].Value;
        FMemTableKaynak.FetchRecord(FMamulKart.TableRotaKaynak);
        if IsSkipped(AMamulData.MalTip) then
        begin
          FMemTableKaynak.ReceteNo := AMamulData.Parent.ReceteNo;
          FMemTableKaynak.RevizyonNo := AMamulData.Parent.RevizyonNo;
        end;
        FMemTableKaynak.CalcCalismaSure := ACalismaSure;
        FMemTableKaynak.CalcToplamSure := AToplamSure;
        FMemTableKaynak.Post;
      end;

      FMamulKart.TableRotaKaynak.Next;
    end;
  end;

  function AddKaynakIslem(ARotaData: TAppROMData): TAppMaliyetData;
  begin
    ResetMaliyetData(Result);

    FKaynakIslem.Open(ARotaData.KaynakKod);
    FKaynakIslem.Table.First;
    while not FKaynakIslem.Table.Eof do
    begin
      KaynakIslemData.ID := NewID;
      KaynakIslemData.ReceteNo := ARotaData.ReceteNo;
      KaynakIslemData.RevizyonNo := ARotaData.RevizyonNo;
      KaynakIslemData.OperasyonNo := ARotaData.OperasyonNo;
      KaynakIslemData.RotaTip := rotMachineProcess;
      KaynakIslemData.IsMerkezKod := ARotaData.IsMerkezKod;
      KaynakIslemData.KaynakKod := ARotaData.KaynakKod;
      KaynakIslemData.SiraNo := FKaynakIslem.Table.IslemNo;
      KaynakIslemData.Aciklama := FKaynakIslem.Table.Aciklama;
      KaynakIslemData.IslemTip := FKaynakIslem.Table.IslemTip;
      KaynakIslemData.OperatorKod := FKaynakIslem.Table.OperatorKod;
      KaynakIslemData.SeviyeKod := ARotaData.SeviyeKod;
      KaynakIslemData.ParentID := ARotaData.ID; // Sadece ağaç gösterimi için

      DoOnReturnRoute(KaynakIslemData);
      FKaynakIslem.Table.Next;
    end;
  end;

  function AddKaynak(ARotaData: TAppROMData): TAppMaliyetData;
  var
    AMinKaynakKod: String;
    AMinToplamSure: Double;
  begin
    ResetMaliyetData(Result);

    FMemTableKaynak.SetRange([ARotaData.ReceteNo, ARotaData.RevizyonNo, ARotaData.OperasyonNo], [ARotaData.ReceteNo, ARotaData.RevizyonNo, ARotaData.OperasyonNo]);

    FMemTableKaynak.First;
    AMinKaynakKod := FMemTableKaynak.KaynakKod;
    AMinToplamSure := -1;
    
    while not FMemTableKaynak.Eof do
    begin
      if (AMinToplamSure = -1) or (FMemTableKaynak.CalcToplamSure < AMinToplamSure) then
      begin
        AMinKaynakKod := FMemTableKaynak.KaynakKod;
        AMinToplamSure := FMemTableKaynak.CalcToplamSure;
      end;
      FMemTableKaynak.Next;
    end;

    FMemTableKaynak.First;
    while not FMemTableKaynak.Eof do
    begin
      KaynakData.ID := NewID;
      KaynakData.ReceteNo := FMemTableKaynak.ReceteNo;
      KaynakData.RevizyonNo := FMemTableKaynak.RevizyonNo;
      KaynakData.OperasyonNo := FMemTableKaynak.OperasyonNo;
      KaynakData.RotaTip := rotMachineCenter;
      KaynakData.IsMerkezKod := FMemTableKaynak.IsMerkezKod;
      KaynakData.KaynakKod := FMemTableKaynak.KaynakKod;
      KaynakData.Aciklama := FMemTableKaynak.KaynakAd;
      KaynakData.SiraNo := FMemTableKaynak.KullanimSiraNo;
      KaynakData.KurulumSure := FMemTableKaynak.KaynakKurulumSure;
      KaynakData.CalismaSure := FMemTableKaynak.CalcCalismaSure;
      KaynakData.SokumSure := FMemTableKaynak.KaynakSokumSure;
      KaynakData.ToplamSure := FMemTableKaynak.CalcToplamSure;
//        KaynakData.Kullanilan := FMemTableKaynak.KullanimTip = 1;
      KaynakData.Kullanilan := AMinKaynakKod = KaynakData.KaynakKod;
      KaynakData.SeviyeKod := ARotaData.SeviyeKod;
      KaynakData.ParentID := ARotaData.ID; // Sadece ağaç gösterimi için

      if Params.ReturnProcess then
        AddKaynakIslem(KaynakData);

      DoOnReturnRoute(KaynakData);
      FMemTableKaynak.Next;
    end;
  end;

  function AddRota(AReceteNo, ARevizyonNo: String; AMamulDataID: Integer; AMamulSeviyeKod: Smallint): TAppMaliyetData;
  var
    ALastOpNo: Smallint;
    ASonrakiOperasyonNo: String;
  begin
    ResetMaliyetData(Result);

    ALastOpNo := 0;
    FMemTableRota.SetRange([AReceteNo, ARevizyonNo], [AReceteNo, ARevizyonNo]);
    FMemTableRota.Last;
    while not FMemTableRota.Bof do
    begin
      RotaData.ID := NewID;
      RotaData.ReceteNo := FMemTableRota.ReceteNo;
      RotaData.RevizyonNo := FMemTableRota.RevizyonNo;
      RotaData.OperasyonNo := FMemTableRota.OperasyonNo;

      FMemTableSonrakiOperasyon.SetRange([RotaData.ReceteNo, RotaData.RevizyonNo, RotaData.OperasyonNo], [RotaData.ReceteNo, RotaData.RevizyonNo, RotaData.OperasyonNo]);
      if FMemTableSonrakiOperasyon.IsEmpty then
      begin
        if ALastOpNo = 0 then
          ASonrakiOperasyonNo := ''
        else
          ASonrakiOperasyonNo := IntToStr(ALastOpNo);
      end
      else begin
        ASonrakiOperasyonNo := '';
        FMemTableSonrakiOperasyon.First;
        while not FMemTableSonrakiOperasyon.Eof do
        begin
          if ASonrakiOperasyonNo <> '' then
            ASonrakiOperasyonNo := ASonrakiOperasyonNo + '|';
          ASonrakiOperasyonNo := ASonrakiOperasyonNo + IntToStr(FMemTableSonrakiOperasyon.SonrakiOperasyonNo);
          FMemTableSonrakiOperasyon.Next;
        end;
      end;
      ALastOpNo := RotaData.OperasyonNo;
      RotaData.SonrakiOperasyonNo := ASonrakiOperasyonNo;

      RotaData.RotaTip := rotWorkCenter;
      RotaData.IsMerkezKod := FMemTableRota.IsMerkezKod;
      RotaData.KaynakKod := '';
      RotaData.Aciklama := FMemTableRota.Aciklama;
      RotaData.SeviyeKod := AMamulSeviyeKod; 
      RotaData.ParentID := AMamulDataID; // Sadece ağaç gösterimi için

      AddKaynak(RotaData);

      DoOnReturnRoute(RotaData);

      FMemTableRota.Prior;
    end;
  end;

  procedure AddHammadde(var AMaliyetData: TAppMaliyetData);
  begin
    if (HammaddeData.MalTip = botCoProduct) and (HammaddeData.YanUrunMaliyetOran >= 0) then // Yan Ürün
    begin
      EndMaliyetData(HammaddeData.Miktar, HammaddeData.Maliyet);
      DoOnReturn(HammaddeData);
    end
    else if HammaddeData.SonSeviye then // Hammadde (Hammadde veya Yarı Mamül) veyahut Yan Ürün
    begin
      // Get Hammadde Maliyet
      dcMaliyet.Execute(HammaddeData.MalKod, HammaddeData.VersiyonNo, HammaddeData.DepoKod, '', '', HammaddeData.Miktar + HammaddeData.FireMiktar, AMaliyetData);
      if MamulKartData.HammaddeStokKartMaliyetGrupNo = 1 then
      begin
        AMaliyetData.MaliyetGrup1 := AMaliyetData.Maliyet;
        AMaliyetData.YerelMaliyetGrup1 := AMaliyetData.YerelMaliyet;
      end
      else if MamulKartData.HammaddeStokKartMaliyetGrupNo = 2 then
      begin
        AMaliyetData.MaliyetGrup2 := AMaliyetData.Maliyet;
        AMaliyetData.YerelMaliyetGrup2 := AMaliyetData.YerelMaliyet;
      end
      else if MamulKartData.HammaddeStokKartMaliyetGrupNo = 3 then
      begin
        AMaliyetData.MaliyetGrup3 := AMaliyetData.Maliyet;
        AMaliyetData.YerelMaliyetGrup3 := AMaliyetData.YerelMaliyet;
      end
      else if MamulKartData.HammaddeStokKartMaliyetGrupNo = 4 then
      begin
        AMaliyetData.MaliyetGrup4 := AMaliyetData.Maliyet;
        AMaliyetData.YerelMaliyetGrup4 := AMaliyetData.YerelMaliyet;
      end else
      begin
        AMaliyetData.MaliyetGrupDiger := AMaliyetData.Maliyet;
        AMaliyetData.YerelMaliyetGrupDiger := AMaliyetData.YerelMaliyet;
      end;
      if (HammaddeData.MalTip <> botCoProduct) and (HammaddeData.YanUrunNo <> 0) then // Yan Ürün No tanımlı bir hammadde ise
      begin
        if FMemTableYanUrun.FindKey([HammaddeData.ReceteNo, HammaddeData.RevizyonNo, HammaddeData.YanUrunNo]) then
        begin
          FMemTableYanUrun.Edit;
          FMemTableYanUrun.Maliyet := FMemTableYanUrun.Maliyet + (AMaliyetData.Maliyet * HammaddeData.YanUrunMaliyetOran / 100);
          FMemTableYanUrun.MaliyetGrup1 := FMemTableYanUrun.MaliyetGrup1 + (AMaliyetData.MaliyetGrup1 * HammaddeData.YanUrunMaliyetOran / 100);
          FMemTableYanUrun.MaliyetGrup2 := FMemTableYanUrun.MaliyetGrup2 + (AMaliyetData.MaliyetGrup2 * HammaddeData.YanUrunMaliyetOran / 100);
          FMemTableYanUrun.MaliyetGrup3 := FMemTableYanUrun.MaliyetGrup3 + (AMaliyetData.MaliyetGrup3 * HammaddeData.YanUrunMaliyetOran / 100);
          FMemTableYanUrun.MaliyetGrup4 := FMemTableYanUrun.MaliyetGrup4 + (AMaliyetData.MaliyetGrup4 * HammaddeData.YanUrunMaliyetOran / 100);
          FMemTableYanUrun.MaliyetGrupDiger := FMemTableYanUrun.MaliyetGrupDiger + (AMaliyetData.MaliyetGrupDiger * HammaddeData.YanUrunMaliyetOran / 100);

          FMemTableYanUrun.YerelMaliyet1 := FMemTableYanUrun.YerelMaliyet1 + (AMaliyetData.YerelMaliyet1 * HammaddeData.YanUrunMaliyetOran / 100);
          FMemTableYanUrun.YerelMaliyet2 := FMemTableYanUrun.YerelMaliyet2 + (AMaliyetData.YerelMaliyet2 * HammaddeData.YanUrunMaliyetOran / 100);
          FMemTableYanUrun.YerelMaliyet3 := FMemTableYanUrun.YerelMaliyet3 + (AMaliyetData.YerelMaliyet3 * HammaddeData.YanUrunMaliyetOran / 100);

          FMemTableYanUrun.YerelMaliyet := FMemTableYanUrun.YerelMaliyet + (AMaliyetData.YerelMaliyet * HammaddeData.YanUrunMaliyetOran / 100);
          FMemTableYanUrun.YerelMaliyetGrup1 := FMemTableYanUrun.YerelMaliyetGrup1 + (AMaliyetData.YerelMaliyetGrup1 * HammaddeData.YanUrunMaliyetOran / 100);
          FMemTableYanUrun.YerelMaliyetGrup2 := FMemTableYanUrun.YerelMaliyetGrup2 + (AMaliyetData.YerelMaliyetGrup2 * HammaddeData.YanUrunMaliyetOran / 100);
          FMemTableYanUrun.YerelMaliyetGrup3 := FMemTableYanUrun.YerelMaliyetGrup3 + (AMaliyetData.YerelMaliyetGrup3 * HammaddeData.YanUrunMaliyetOran / 100);
          FMemTableYanUrun.YerelMaliyetGrup4 := FMemTableYanUrun.YerelMaliyetGrup4 + (AMaliyetData.YerelMaliyetGrup4 * HammaddeData.YanUrunMaliyetOran / 100);
          FMemTableYanUrun.YerelMaliyetGrupDiger := FMemTableYanUrun.YerelMaliyetGrupDiger + (AMaliyetData.YerelMaliyetGrupDiger * HammaddeData.YanUrunMaliyetOran / 100);
          FMemTableYanUrun.Post;
        end;
      end;

      // Return Hammadde Maliyet
      HammaddeData.Maliyet := AMaliyetData;
      DoOnReturn(HammaddeData);
    end else // Assembly (Mamül, Yarı Mamül veya Hayalet Montaj)
      ChildMamulList.Add(HammaddeData);
  end;

  function CalcScrap(Miktar: Double): Double;
  begin
    // Montaj Firesini Ekle //
    if MamulKartData.OperasyonFireKullan = 1 then // Operasyon Firesini Kullan
    begin
      // Firenin firesini almaması için önce oranı al sonra sabit miktarı ekle.
      Result := Miktar * MamulKartData.OperasyonFireOran / 100;
      Result := Result + MamulKartData.OperasyonFireMiktar;
    end else // Montaj Firesini Kullan
      Result := Miktar * MamulKartData.MamulStokKartMontajFireOran / 100;

    // Bileşen Firesini Ekle //
    if MamulKartData.BilesenFireOran > 0 then // Mamül Kart tablosuna Bileşen Fire Oranı Girilmiş İse
      Result := Result + (Miktar * MamulKartData.BilesenFireOran / 100)
    else // Hammadde Tablosuna girilmemiş ise stok kart da yazan bileşen fire oranını kullan.
      Result := Result + (Miktar * MamulKartData.HammaddeStokKartBilesenFireOran / 100);
  end;

  function AddChild: TAppMaliyetData;
  var
    vMamulMiktar, vHammaddeMiktar: Double;
    vFireMiktar, vLotFireMiktar, vKullFireMiktar: Double;
    vYanUrunMiktar, vLotYanUrunMiktar, vKullYanUrunMiktar: Double;
    I: Integer;
  begin
    ResetMaliyetData(Result);

    // Mamül Birim Çevrim //
    vMamulMiktar := MamulData.Miktar;

    if MamulData.Birim <> MamulKartData.MamulBirim then // İstenen mamülün birimi mamül ağacı birimi ile aynı ise çevrime gerek yok.
    begin
      if MamulData.Birim <> MamulKartData.MamulStokKartBirim then // Önce istenen mamülün birimi stok birimine çevir
      begin
        if dcCommon.StokBirim.Find(MamulData.MalKod, MamulData.Birim) and (dcCommon.StokBirim.Table.Katsayi > 0) then
          vMamulMiktar := vMamulMiktar * dcCommon.StokBirim.Table.Katsayi
        else
          DoOnReturnError('BOM Explosion', ErrCode_StokBirimCevrimNotFound, format('%s nolu mamülün mamül başlığında tanımlı %s biriminin stok kart birimine çevrim kat sayısı stok birim tablosunda bulunamadı', [MamulData.MalKod, MamulData.Birim]), MamulData);
      end;

      if MamulKartData.MamulBirim <> MamulKartData.MamulStokKartBirim then // Sonra stok birimini mamül ağacı birimine çevir
      begin
        if MamulKartData.MamulStokBirimKatsayi > 0 then
          vMamulMiktar := vMamulMiktar * MamulKartData.MamulStokBirimKatsayi
        else
          DoOnReturnError('BOM Explosion', ErrCode_StokBirimCevrimNotFound, format('%s nolu mamülün mamül başlığında tanımlı %s biriminin stok kart birimine çevrim kat sayısı stok birim tablosunda bulunamadı', [MamulData.MalKod, MamulKartData.MamulBirim]), MamulData);
      end;
    end;

    // Hammadde Birim Çevrim //
    vHammaddeMiktar := MamulKartData.Miktar;
    vYanUrunMiktar := MamulKartData.YanUrunMiktar;
    if MamulKartData.HammaddeBirim <> MamulKartData.HammaddeStokKartBirim then
    begin
      if MamulKartData.HammaddeStokBirimKatsayi > 0 then
      begin
        vHammaddeMiktar := vHammaddeMiktar * MamulKartData.HammaddeStokBirimKatsayi;
        vYanUrunMiktar := vYanUrunMiktar * MamulKartData.HammaddeStokBirimKatsayi;
      end else
        DoOnReturnError('BOM Explosion', ErrCode_StokBirimCevrimNotFound, format('%s nolu hammaddenin %s nolu mamül kartında tanımlı %s biriminin stok kart birimine çevrim kat sayısı stok birim tablosunda bulunamadı', [MamulKartData.HammaddeKod, MamulData.MalKod, MamulKartData.HammaddeBirim]), MamulData);
    end;

    // Mamülün Hammadde İhtiyacını bul //
    if MamulKartData.MiktarTip = 0 then // Oransal
    begin
      vHammaddeMiktar := vHammaddeMiktar * vMamulMiktar;
      vYanUrunMiktar := vYanUrunMiktar * vMamulMiktar;
    end;

    // Yuvarla //
    if Params.Round then
    begin
      vHammaddeMiktar := AppRoundToUp(vHammaddeMiktar, MamulKartData.HammaddeStokKartYuvarlama);
      vYanUrunMiktar := AppRoundToUp(vYanUrunMiktar, MamulKartData.HammaddeStokKartYuvarlama);
    end;

    // Fire Hesapla //
    if Params.CalcScrap > 0 then
    begin
      vFireMiktar := CalcScrap(vHammaddeMiktar);

      // Fire Miktar Yuvarla //
      if Params.Round then
        vFireMiktar := AppRoundToUp(vFireMiktar, MamulKartData.HammaddeStokKartYuvarlama);

      vHammaddeMiktar := vHammaddeMiktar + vFireMiktar;
    end else
      vFireMiktar := 0;

    HammaddeData.ID := NewID;
    HammaddeData.ReceteNo := MamulKartData.ReceteNo;
    HammaddeData.RevizyonNo := MamulKartData.RevizyonNo;
    HammaddeData.ReceteSiraNo := MamulKartData.ReceteSiraNo;

    if MamulKartData.CoProduct then
      HammaddeData.MalTip := botCoProduct
    else
    begin
      if MamulKartData.HayaletMontaj then
        HammaddeData.MalTip := botPhantomAssembly
      else // Others
      begin
        if MamulKartData.HammaddeStokKartTeminYontem = Integer(tyInternal) then // Üretim
          HammaddeData.MalTip := botProduct
        else if MamulKartData.HammaddeStokKartTeminYontem = Integer(tyPhantom) then // Hayalet Montaj
          HammaddeData.MalTip := botPhantomAssembly
        else // tyExternal / Satınalma
          HammaddeData.MalTip := botRowMaterial;
      end;
    end;

    HammaddeData.MalKod := MamulKartData.HammaddeKod;
    HammaddeData.VersiyonNo := MamulKartData.HammaddeVersiyonNo;
    HammaddeData.SurumNo := MamulKartData.HammaddeSurumNo;

    if MamulKartData.DepoKod <> '' then
      HammaddeData.DepoKod := MamulKartData.DepoKod
    else if Params.HammaddeDepoKod <> '' then
      HammaddeData.DepoKod := Params.HammaddeDepoKod
    else if Params.MamulDepoKod <> '' then
      HammaddeData.DepoKod := Params.MamulDepoKod
    else if MamulKartData.MamulHammaddeDepoKod <> '' then
      HammaddeData.DepoKod := MamulKartData.MamulHammaddeDepoKod
    else
      HammaddeData.DepoKod := MamulKartData.MamulDepoKod;

    HammaddeData.SeriNo := '';
    HammaddeData.SeriNo2 := '';

    HammaddeData.PozNo := MamulKartData.PozNo;
    HammaddeData.YanUrunNo := MamulKartData.YanUrunNo;
    if HammaddeData.MalTip = botCoProduct then // Yan Ürünler için
    begin
      if MamulKartData.YanUrunMaliyetTip = 0 then // Kendi Maliyeti
        HammaddeData.YanUrunMaliyetOran := -1
      else // Oransal
        HammaddeData.YanUrunMaliyetOran := MamulKartData.YanUrunMaliyetOran;
    end else // Hammaddeler için
    begin
      if MamulKartData.YanUrunMaliyetTip = 0 then // Oransal
        HammaddeData.YanUrunMaliyetOran := MamulKartData.YanUrunMaliyetOran
      else // Miktarsal
        HammaddeData.YanUrunMaliyetOran := (vYanUrunMiktar / vHammaddeMiktar) * 100;
    end;
    HammaddeData.OperasyonNo := MamulKartData.OperasyonNo;
    HammaddeData.KaynakIslemNo := MamulKartData.KaynakIslemNo;
    HammaddeData.Birim := MamulKartData.HammaddeStokKartBirim;

    HammaddeData.TeminTip := MamulKartData.HammaddeStokKartTeminTip;
    HammaddeData.TeminYontem := MamulKartData.HammaddeStokKartTeminYontem;
    HammaddeData.Tarih := MamulData.Tarih;

    HammaddeData.KodUretim := MamulKartData.KodUretim;

    if Params.SingleLevel then
      HammaddeData.SonSeviye := HammaddeData.MalTip <> botPhantomAssembly
    else
      HammaddeData.SonSeviye := HammaddeData.MalTip in [botRowMaterial, botCoProduct];

    if Params.SipariseUretim then // Siparişe üretim ise yarı mamülün üretim yönteminin siparişe üretim olup olmadığı önemli
      if not HammaddeData.SonSeviye then
        HammaddeData.SonSeviye := MamulKartData.HammaddeStokKartTeminTip <> Integer(ttOrder); // Siparişe üretim değil ise hammadde seviyesidir, daha alta inme

    if IsSkipped(MamulData.MalTip) then
    begin
      HammaddeData.SeviyeKod := MamulData.SeviyeKod;
      HammaddeData.Parent := MamulData.Parent;
    end else
    begin
      HammaddeData.SeviyeKod := MamulData.SeviyeKod + 1;
      HammaddeData.Parent := MamulData;
    end;

    HammaddeData.ReqType := Params.ReqType;

    if MatchFields.TableDes <> nil then // TableDes verilmişse match field yap
    begin
      for I := 0 to MatchFields.FieldSrcList.Count - 1 do
        if Assigned(MatchFields.FieldSrcList.Objects[I]) then
          HammaddeData.MatchValues[I] := TField(MatchFields.FieldSrcList.Objects[I]).Value;
    end;

    if Params.LotKapat and (HammaddeData.MalTip <> botPhantomAssembly) and (HammaddeData.MalTip <> botCoProduct) then // Alternatiflerin çalışabilmesi için lot kapat parametresi true olmak zorunda.
    begin
      dcLotKapat.GetLotMiktar(HammaddeData.MalKod, HammaddeData.VersiyonNo, HammaddeData.DepoKod, '', '', vHammaddeMiktar);
      dcLotKapat.TableLot.First;
      while not dcLotKapat.TableLot.Eof do
      begin
        HammaddeData.MalKod := dcLotKapat.TableLot.MalKod;
        HammaddeData.VersiyonNo := dcLotKapat.TableLot.VersiyonNo;
        if (HammaddeData.MalKod = dcLotKapat.TableLot.MalKod) and (HammaddeData.VersiyonNo = dcLotKapat.TableLot.VersiyonNo) then // Yarı mamülse ve alternatifi kullanılmışsa.
          HammaddeData.SurumNo := MamulKartData.HammaddeSurumNo
        else
          HammaddeData.SurumNo := 0;
        HammaddeData.SeriNo := dcLotKapat.TableLot.SeriNo;
        HammaddeData.SeriNo2 := dcLotKapat.TableLot.SeriNo2;
        HammaddeData.Miktar := dcLotKapat.TableLot.Kullanilabilir;

        // Calc Lot Fire Miktar
        vLotFireMiktar := vFireMiktar * (HammaddeData.Miktar / vHammaddeMiktar);
        if Params.Round then
          vLotFireMiktar := AppRoundToUp(vLotFireMiktar, MamulKartData.HammaddeStokKartYuvarlama);
        if vLotFireMiktar < vFireMiktar then
          vKullFireMiktar := vLotFireMiktar
        else
          vKullFireMiktar := vFireMiktar;
        vFireMiktar := vFireMiktar - vKullFireMiktar;
        HammaddeData.FireMiktar := vKullFireMiktar;

        AddHammadde(Result);

        dcLotKapat.TableLot.Next;
      end;
    end else
    begin
      HammaddeData.Miktar := vHammaddeMiktar;
      HammaddeData.FireMiktar := vFireMiktar;
      AddHammadde(Result);
    end;
  end;

var
  I: Integer;
  vHammaddeMaliyet, vYanUrunMaliyet: TAppMaliyetData;
  vReceteNo, vRevizyonNo: String;
  vHasOzelRecete: Boolean;
begin
  ResetMaliyetData(Result);

  ChildMamulList := TAppBOMDataList.Create;
  try
    try
      FMamulKart.Open(Params.KullanimGrupNo, MamulData.MalKod, MamulData.VersiyonNo, MamulData.KullanimKod, MamulData.SurumNo, MamulData.Tarih, vReceteNo, vRevizyonNo);
      CheckMemTables;
      FetchMemYanUrun(MamulData);

      vHasOzelRecete := False;
      if FParams.OzelReceteTip = Integer(ortOzelRecete) then
      begin
        if Assigned(TableOzelMamulKart) then
        begin
          vOzelMamulKart := TableOzelMamulKart;
          // TAppDataControllerEvrakUretimOzelBaglanti.BOMTree.TableOzelMamulKart = TableBaglantiOzelMamulKart and its index is 'EVRAKSIRANO;MAMULKOD;MAMULVERSIYONNO;MAMULSURUMNO';
          // TAppDataControllerEvrakUretim.BOMTree.TableOzelMamulKart = DataChildOzelMamulKart.Table and its index is 'EVRAKSIRANO;SIRANO'; and there is no need to setrange
          if vOzelMamulKart.IndexFieldNames = 'EVRAKSIRANO;MAMULKOD;MAMULVERSIYONNO;MAMULSURUMNO' then
            vOzelMamulKart.SetRange([Params.EvrakSiraNo, MamulData.MalKod, MamulData.VersiyonNo, MamulData.SurumNo],[Params.EvrakSiraNo, MamulData.MalKod, MamulData.VersiyonNo, MamulData.SurumNo]);
        end else
        begin
          vOzelMamulKart := FOzelMamulKart.Table;
          FOzelMamulKart.Open(Params.EvrakTip, Params.HesapKod, Params.EvrakNo, Params.EvrakSiraNo, MamulData.MalKod, MamulData.VersiyonNo, MamulData.SurumNo);
        end;
        vOzelMamulKart.First;
        if vOzelMamulKart.Eof then
          DoOnReturnError('BOM Explosion', ErrCode_MamulKartNotFound, format('%s nolu malın %s nolu versiyonunun özel mamül kartı bulunamadı.', [MamulData.MalKod, MamulData.VersiyonNo]), MamulData)
        else begin
          vHasOzelRecete := True;
          for I := 0 to MatchFields.FieldSrcList.Count - 1 do
            MatchFields.FieldSrcList.Objects[I] := vOzelMamulKart.FindField(MatchFields.FieldSrcList[I]);
        end;
        while not vOzelMamulKart.Eof do
        begin
          if IsValidItem(AppFirstDate, AppFirstDate, vOzelMamulKart.HammaddeStokKartMRPTip) then
          begin
            FetchOzelMamulKart;
            vHammaddeMaliyet := AddChild;
            IncMaliyetData(vHammaddeMaliyet, Result);
          end;
          vOzelMamulKart.Next;
        end;
      end;

      if not vHasOzelRecete then
      begin
        FMamulKart.TableKart.First;
        if FMamulKart.TableKart.Eof then
          DoOnReturnError('BOM Explosion', ErrCode_MamulKartNotFound, format('%s nolu malın %s nolu versiyonunun mamül kartı bulunamadı.', [MamulData.MalKod, MamulData.VersiyonNo]), MamulData)
        else
        begin
          for I := 0 to MatchFields.FieldSrcList.Count - 1 do
            MatchFields.FieldSrcList.Objects[I] := FMamulKart.TableKart.FindField(MatchFields.FieldSrcList[I]);
        end;
        while not FMamulKart.TableKart.Eof do
        begin
          if IsValidItem(FMamulKart.TableKart.BasTarih, FMamulKart.TableKart.BitTarih, FMamulKart.TableKart.HammaddeStokKartMRPTip) then
          begin
            FetchMamulKart;
            vHammaddeMaliyet := AddChild;
            IncMaliyetData(vHammaddeMaliyet, Result);
          end;
          FMamulKart.TableKart.Next;
        end;
      end;

      // Fetch Rota
      if Params.ReturnRoute then
      begin
        FetchMemRota(MamulData);
        FetchMemSonrakiOperasyon(MamulData);
        FetchMemKaynak(MamulData);
      end;

      // Burada döngü yapıyorum
      if ChildMamulList.FList.Count > 0 then
      begin
        for I := 0 to ChildMamulList.FList.Count - 1 do
        begin
          vHammaddeMaliyet := InternalExpand(ChildMamulList.FList[I]);
          IncMaliyetData(vHammaddeMaliyet, Result);
        end;
      end;
    except on E:Exception do
      DoOnReturnError('BOM Explosion', ErrCode_MamulAgacUnknownError, E.ClassName + ': ' + E.Message, MamulData);
    end;

    if not IsSkipped(MamulData.MalTip) then
    begin
      if Params.ReturnRoute then
      begin
        vHammaddeMaliyet := AddRota(vReceteNo, vRevizyonNo, MamulData.ID, MamulData.SeviyeKod);
        IncMaliyetData(vHammaddeMaliyet, Result);
      end;

      // Add Yan Ürün
      FMemTableYanUrun.SetRange([vReceteNo, vRevizyonNo], [vReceteNo, vRevizyonNo]);
      FMemTableYanUrun.First;
      while not FMemTableYanUrun.Eof do
      begin
        FetchMamulYanUrun;

        // Calc Yan Ürün Maliyet
        if FMemTableYanUrun.MaliyetTip = 1 then // Oransal
        begin
          if FMemTableYanUrun.MaliyetOran = 0 then
          begin
            HammaddeData.Maliyet.Maliyet := FMemTableYanUrun.Maliyet;
            HammaddeData.Maliyet.MaliyetGrup1 := FMemTableYanUrun.MaliyetGrup1;
            HammaddeData.Maliyet.MaliyetGrup2 := FMemTableYanUrun.MaliyetGrup2;
            HammaddeData.Maliyet.MaliyetGrup3 := FMemTableYanUrun.MaliyetGrup3;
            HammaddeData.Maliyet.MaliyetGrup4 := FMemTableYanUrun.MaliyetGrup4;
            HammaddeData.Maliyet.MaliyetGrupDiger := FMemTableYanUrun.MaliyetGrupDiger;
            HammaddeData.Maliyet.YerelMaliyet1 := FMemTableYanUrun.YerelMaliyet1;
            HammaddeData.Maliyet.YerelMaliyet2 := FMemTableYanUrun.YerelMaliyet2;
            HammaddeData.Maliyet.YerelMaliyet3 := FMemTableYanUrun.YerelMaliyet3;
            HammaddeData.Maliyet.YerelMaliyet := FMemTableYanUrun.YerelMaliyet;
            HammaddeData.Maliyet.YerelMaliyetGrup1 := FMemTableYanUrun.YerelMaliyetGrup1;
            HammaddeData.Maliyet.YerelMaliyetGrup2 := FMemTableYanUrun.YerelMaliyetGrup2;
            HammaddeData.Maliyet.YerelMaliyetGrup3 := FMemTableYanUrun.YerelMaliyetGrup3;
            HammaddeData.Maliyet.YerelMaliyetGrup4 := FMemTableYanUrun.YerelMaliyetGrup4;
            HammaddeData.Maliyet.YerelMaliyetGrupDiger := FMemTableYanUrun.YerelMaliyetGrupDiger;
          end else
          begin
            HammaddeData.Maliyet.Maliyet := Result.Maliyet * FMemTableYanUrun.MaliyetOran / 100;
            HammaddeData.Maliyet.MaliyetGrup1 := Result.MaliyetGrup1 * FMemTableYanUrun.MaliyetOran / 100;
            HammaddeData.Maliyet.MaliyetGrup2 := Result.MaliyetGrup2 * FMemTableYanUrun.MaliyetOran / 100;
            HammaddeData.Maliyet.MaliyetGrup3 := Result.MaliyetGrup3 * FMemTableYanUrun.MaliyetOran / 100;
            HammaddeData.Maliyet.MaliyetGrup4 := Result.MaliyetGrup4 * FMemTableYanUrun.MaliyetOran / 100;
            HammaddeData.Maliyet.MaliyetGrupDiger := Result.MaliyetGrupDiger * FMemTableYanUrun.MaliyetOran / 100;
            HammaddeData.Maliyet.YerelMaliyet1 := Result.YerelMaliyet1 * FMemTableYanUrun.MaliyetOran / 100;
            HammaddeData.Maliyet.YerelMaliyet2 := Result.YerelMaliyet2 * FMemTableYanUrun.MaliyetOran / 100;
            HammaddeData.Maliyet.YerelMaliyet3 := Result.YerelMaliyet3 * FMemTableYanUrun.MaliyetOran / 100;
            HammaddeData.Maliyet.YerelMaliyet := Result.YerelMaliyet * FMemTableYanUrun.MaliyetOran / 100;
            HammaddeData.Maliyet.YerelMaliyetGrup1 := Result.YerelMaliyetGrup1 * FMemTableYanUrun.MaliyetOran / 100;
            HammaddeData.Maliyet.YerelMaliyetGrup2 := Result.YerelMaliyetGrup2 * FMemTableYanUrun.MaliyetOran / 100;
            HammaddeData.Maliyet.YerelMaliyetGrup3 := Result.YerelMaliyetGrup3 * FMemTableYanUrun.MaliyetOran / 100;
            HammaddeData.Maliyet.YerelMaliyetGrup4 := Result.YerelMaliyetGrup4 * FMemTableYanUrun.MaliyetOran / 100;
            HammaddeData.Maliyet.YerelMaliyetGrupDiger := Result.YerelMaliyetGrupDiger * FMemTableYanUrun.MaliyetOran / 100;
          end;
        end;

        // Return Yan Ürün
        AddChild;

        // Add To Yan Ürün Maliyet Toplam
        IncMaliyetData(HammaddeData.Maliyet, vYanUrunMaliyet);

        FMemTableYanUrun.Next;
      end;

      // Hammadde Maliyet - Yan Ürün Maliyet
      DecMaliyetData(vYanUrunMaliyet, Result);

      // Add Ana Ürün
      EndMaliyetData(MamulData.Miktar, Result);

      MamulData.Maliyet := Result;
      if MamulData.SeviyeKod = 0 then
        dcMaliyet.Save(MamulData.MalKod, MamulData.VersiyonNo, MamulData.Maliyet);

      DoOnReturn(MamulData);
    end;
  finally
    ChildMamulList.Free;
  end;
end;

procedure TAppDataControllerBOMTree.InternalWhereUsed(Data: TAppBOMData);
var
  ChildMamulList: TAppBOMDataList;

  procedure AddChild;
  var
    MamulMiktar, HammaddeMiktar: Double;
  begin
    // Mamül Birim Çevrim //
    MamulMiktar := Data.Miktar;

    if FMamulKart.TableKart.HammaddeBirim <> FMamulKart.TableKart.HammaddeStokKartBirim then // Mamül ağacı birimi stok biriminden farklı ise mamül ağacı birimine çevrilir.
    begin
      if FMamulKart.TableKart.HammaddeStokBirimKatsayi > 0 then
        MamulMiktar := MamulMiktar * FMamulKart.TableKart.HammaddeStokBirimKatsayi
      else
        DoOnReturnError('BOM Explosion', ErrCode_StokBirimCevrimNotFound, format('%s nolu mamülün mamül başlığında tanımlı %s biriminin stok kart birimine çevrim kat sayısı stok birim tablosunda bulunamadı', [FMamulKart.TableKart.MamulKod, FMamulKart.TableKart.MamulBirim]), Data);
    end;

    // Hammadde Birim Çevrim //
    HammaddeMiktar := FMamulKart.TableKart.Miktar;
    if FMamulKart.TableKart.MamulBirim <> FMamulKart.TableKart.MamulStokKartBirim then
    begin
      if FMamulKart.TableKart.MamulStokBirimKatsayi > 0 then
        HammaddeMiktar := HammaddeMiktar * FMamulKart.TableKart.MamulStokBirimKatsayi
      else
        DoOnReturnError('BOM Explosion', ErrCode_StokBirimCevrimNotFound, format('%s nolu hammaddenin %s nolu mamül kartında tanımlı %s biriminin stok kart birimine çevrim kat sayısı stok birim tablosunda bulunamadı', [FMamulKart.TableKart.HammaddeKod, FMamulKart.TableKart.MamulKod, FMamulKart.TableKart.HammaddeBirim]), Data);
    end;

    // Mamülün Hammadde İhtiyacını bul //
    if FMamulKart.TableKart.MiktarTip = 0 then // Oransal
      HammaddeMiktar := HammaddeMiktar * MamulMiktar;

    // Montaj Firesini Ekle //
    if FMamulKart.TableKart.OperasyonFireKullan = 1 then // Operasyon Firesini Kullan
    begin
      // Firenin firesini almaması için önce oranı al sonra sabit miktarı ekle.
      HammaddeMiktar := HammaddeMiktar + (HammaddeMiktar * FMamulKart.TableKart.OperasyonFireOran / 100);
      HammaddeMiktar := HammaddeMiktar + FMamulKart.TableKart.OperasyonFireMiktar;
    end else // Montaj Firesini Kullan
      HammaddeMiktar := HammaddeMiktar + (HammaddeMiktar * FMamulKart.TableKart.MamulStokKartMontajFireOran / 100);

    // Bileşen Firesini Ekle //
    if FMamulKart.TableKart.BilesenFireOran > 0 then // Mamül Kart tablosuna Bileşen Fire Oranı Girilmiş İse
      HammaddeMiktar := HammaddeMiktar + (HammaddeMiktar * FMamulKart.TableKart.BilesenFireOran / 100)
    else // Hammadde Tablosuna girilmemiş ise stok kart da yazan bileşen fire oranını kullan.
      HammaddeMiktar := HammaddeMiktar + (HammaddeMiktar * FMamulKart.TableKart.HammaddeStokKartBilesenFireOran / 100);

    // Yuvarla //
    HammaddeMiktar :=  AppRoundToUp(HammaddeMiktar, FMamulKart.TableKart.MamulStokKartYuvarlama);

    HammaddeData.ID := NewID;
    HammaddeData.MalTip := botProduct;
    HammaddeData.MalKod := FMamulKart.TableKart.MamulKod;
    HammaddeData.VersiyonNo := FMamulKart.TableKart.VersiyonNo;
    HammaddeData.Miktar := HammaddeMiktar;
    HammaddeData.Birim := FMamulKart.TableKart.MamulStokKartBirim;
    HammaddeData.Parent := Data;

    ChildMamulList.Add(HammaddeData);
  end;
var
  I: Integer;
begin
  ChildMamulList := TAppBOMDataList.Create;
  try

    try
      FMamulKart.OpenReverse(Data.MalKod, Data.VersiyonNo);
      FMamulKart.TableKart.First;
      if FMamulKart.TableKart.Eof then
        DoOnReturnError('BOM Explosion', ErrCode_MamulKartNotFound, format('%s nolu malın %s nolu versiyonunun mamül kartı bulunamadı.', [Data.MalKod, Data.VersiyonNo]), Data);
      while not FMamulKart.TableKart.Eof do
      begin
        AddChild;
        FMamulKart.TableKart.Next;
      end;

      // Burada döngü yapıyorum
      if ChildMamulList.FList.Count > 0 then
        for I := 0 to ChildMamulList.FList.Count - 1 do
          InternalWhereUsed(ChildMamulList.FList[I]);

    except on E:Exception do
      DoOnReturnError('BOM Explosion', ErrCode_MamulAgacUnknownError, E.ClassName + ': ' + E.Message, Data);
    end;

    DoOnReturn(Data);
  finally
    ChildMamulList.Free;
  end;
end;

procedure TAppDataControllerBOMTree.Expand(MamulData: TAppBOMData);
begin
  if FMemTableYanUrun.Active then
    FMemTableYanUrun.EmptyTable;
  if FMemTableRota.Active then
    FMemTableRota.EmptyTable;
  if FMemTableSonrakiOperasyon.Active then
    FMemTableSonrakiOperasyon.EmptyTable;
  if FMemTableKaynak.Active then
    FMemTableKaynak.EmptyTable;

  if (MatchFields.TableDes <> nil) and (not MatchFields.IsLoaded) then
  begin
    MatchFields.LoadFields;
    MatchFields.Init;
    SetLength(HammaddeData.MatchValues, MatchFields.FieldSrcList.Count);
  end;

  InternalExpand(MamulData);
end;

procedure TAppDataControllerBOMTree.Expand(MamulKod, VersiyonNo, KullanimKod: string; SurumNo: Integer; Tarih: TDateTime; Miktar: Double; Birim, MamulDepoKod, HammaddeDepoKod: string; OzelReceteTip, EvrakTip: Smallint; HesapKod, EvrakNo: String; EvrakSiraNo: Integer);
var
  AData: TAppBOMData;
begin
  AData := TAppBOMData.Create;
  try
    AData.ID := NewID;

    AData.MalTip := botProduct;
    AData.MalKod := MamulKod;
    AData.VersiyonNo := VersiyonNo;
    AData.KullanimKod := KullanimKod;
    AData.SurumNo := SurumNo;

    AData.Tarih := Tarih;
    AData.Miktar := Miktar;
    AData.Birim := Birim;

    AData.SeviyeKod := 0; // 0 - Kendisi 1- Birinci Seviye ve 2 den sonra diğer seviyeler

    Params.FMamulDepoKod := MamulDepoKod;
    Params.FHammaddeDepoKod := HammaddeDepoKod;
    Params.FOzelReceteTip := OzelReceteTip;
    Params.FEvrakTip := EvrakTip;
    Params.FHesapKod := HesapKod;
    Params.FEvrakNo := EvrakNo;
    Params.FEvrakSiraNo := EvrakSiraNo;

    Expand(AData);
  finally
    FreeAndNil(AData);
  end;
end;

procedure TAppDataControllerBOMTree.Expand(MamulKod, VersiyonNo, KullanimKod: string; SurumNo: Integer; Tarih: TDateTime; Miktar: Double; Birim, MamulDepoKod, HammaddeDepoKod: string);
begin
  Expand(MamulKod, VersiyonNo, KullanimKod, SurumNo, Tarih, Miktar, Birim, MamulDepoKod, HammaddeDepoKod, 0, 0, '', '', 0);
end;

procedure TAppDataControllerBOMTree.Expand(StokKartFilterText: string; StokKartFieldList: TStrings);
  procedure PrepStatus;
  begin
    FStatus.MainForm := nil;
    FStatus.Items.Clear;
    with FStatus.Items.Add do
    begin
      Name := 'MALKOD';
      Caption := 'Mal Kodu';
    end;
    with FStatus.Items.Add do
    begin
      Name := 'MALAD';
      Caption := 'Mal Adı';
    end;
  end;
var
  I: Integer;
begin
  PrepStatus;
  try
    FStokKart.Close;
    FStokKart.Table.TableItems.TableNames := 'STKKRT';
    with FStokKart.Table.TableItems[0] do
    begin
      with Fields do
      begin
        Clear;
        Add('MALKOD');
        Add('MALAD');
        Add('BIRIM');
        if StokKartFieldList.Count > 0 then
        begin
          for I := 0 to StokKartFieldList.Count - 1 do
          begin
            if (StokKartFieldList[I] <> 'MALKOD') and (StokKartFieldList[I] <> 'MALAD') and (StokKartFieldList[I] <> 'BIRIM') then
              Add(StokKartFieldList[I]);
          end;
        end;
      end;
      with Where do
      begin
        Clear;
        if StokKartFilterText <> '' then
          AddText(StokKartFilterText);
      end;
    end;
    FStokKart.Open;
    if not AppConfirm(format('%d adet mal patlatılacak. Devam et?', [FStokKart.Table.RecordCount])) then
      raise Exception.Create('İşlem iptal edildi.');

    if Params.TopluIslem then
    begin
      FStatus.Start(0);
      FStatus.Add('Ürün ağacı açılıyor, lütfen bekleyiniz...');
      FMamulKart.OpenAll;
      FStatus.Reset(FStokKart.Table.RecordCount);
    end else
      FStatus.Start(FStokKart.Table.RecordCount);

    Start;
    try
      FStokKart.Table.First;
      while not FStokKart.Table.Eof do
      begin
        FStatus['MALKOD'] := FStokKart.Table.MalKod;
        FStatus['MALAD'] := FStokKart.Table.MalAd;
        FStatus.Add('Ürün ağacı patlatılıyor...');
        Expand(FStokKart.Table.MalKod, '', '', 0, Date, 1, FStokKart.Table.Birim, '', '');
        FStokKart.Table.Next;
      end;
    finally
      FStatus.Finish;
      Finish;
    end;
  finally
  end;
end;

procedure TAppDataControllerBOMTree.Finish;
begin
  dcMaliyet.Finish;
end;

end.

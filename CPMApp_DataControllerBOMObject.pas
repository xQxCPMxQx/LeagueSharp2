unit CPMApp_DataControllerBOMObject;

interface

uses Windows, Classes, SysUtils, DB, Dialogs, DateUtils, Variants,
  CPMApp_DB, CPMApp_DataObject, CPMApp_TableItems,
  CPMApp_BOMConsts,
  CPMApp_CorporateTables,
  CPMApp_TableBOMSystem, CPMApp_TableMRPSystem,
  CPMApp_TableStokSystem, CPMApp_TableCariSystem,
  CPMApp_TableWarehouseSystem;

type
  // ********* T Tablo adı anlamında I ise Index anlamında ****************
  TAppDataControllerBOMObject = class;
  TAppBOMObjectChild = class;

  TAppBOMChildStokKart = class; // (Stok Kart) (T:STKKRT) (I:MALKOD)
  TAppBOMChildStokKartVersiyon = class; // (Stok Kart) (T:STKKRT) (I:MALKOD)
  TAppBOMChildStokKartBirim = class; // (Stok Kart Birim) (T:STKBRM) (I:MALKOD;BIRIM)
  TAppBOMChildStokKartAlternatif = class; // (Stok Kart Alternatif) (T:STKALT) (I:MALKOD;VERSIYONNO;SIRANO)
  TAppBOMChildCariKart = class; // (Cari Kart) (T:CARKRT) (I:HESAPKOD)
  TAppBOMChildHesapStokKart = class; // (Hesap Stok Kart) (T:STKHMK) (I:MALKOD;VERSIYONNO;HESAPKOD)
  TAppBOMChildMRPAlanKart = class; // (MRPAlan) (T:MRPALN) (I:MRPALANKOD)
  TAppBOMChildMRPAlanStokKart = class; // (MRPAlan Stok Kart) (T:STKHMK) (I:MALKOD;VERSIYONNO;MRPALANKOD)
  TAppBOMChildDepoKart = class; // (Depo Kart) (T:DEPKRT) (I:DEPOKOD)
  TAppBOMChildDepoStokKart = class; // (Depo Stok Kart) (T:STKHMK) (I:MALKOD;VERSIYONNO;DEPOKOD)
  TAppBOMChildMRPParametre = class; // (MRP Parametre) (T:MRPPRM) (I:SIRKETNO)

  TAppBOMChildIsMerkezKart = class; // (İş Merkez Kartı) (T:URTISM) (I:ISMERKEZKOD)
  TAppBOMChildKaynakKart = class; // (Kaynak kartı) (T:URTKYN) (I:KAYNAKKOD)
  TAppBOMChildIsMerkezKaynakKart = class; // (İş Merkezlerindeki Kaynaklar) (T:URTISK) (I:ISMERKEZKOD;KULLANIMSIRANO)
  TAppBOMChildMamulBaslik = class; // (Mamül Başlık) (T:MAMBAS) (I:MAMULKOD;VERSIYONNO;SURUMNO)
  TAppBOMChildMamulKart = class; // (Mamül Kartı) (T:MAMKRT) (I:MAMULKOD;VERSIYONNO;SIRANO;ID)
  TAppBOMChildOzelMamulKart = class; // (Özel Mamül Kartı) (T:STHOMK) (I:SIRANO)
  TAppBOMChildMamulRotaKart = class; // (Mamül Rotaları) (T:MAMROT) (I:OPERASYONNO)
  TAppBOMChildMamulRotaKaynakKart = class; // (Mamül Özel Kaynak) (T:MAMKYN) (I: KULLANIMSIRANO)
  TAppBOMChildTakvimVardiyaKart = class; // (Takvim Vardiyaları) (T:URTTKV) (I: TAKVIMKOD)
  TAppBOMChildKaynakIslemTanim = class; // (Kaynak İşlem Tanımları) (T:URTKIT) (I:ISLEMNO)
  TAppBOMChildKaynakMamulDegisim = class; // (Mamüllerin Değişim Kartı) (T:URTKMD) (I:KURULUMMAMULKOD;SOKUMMAMULKOD)
  TAppBOMChildKaynakMamulKurulum = class; // (Mamüllerin Kurulum Kartı) (T:URTKMK) (I:KAYNAKKOD;MAMULKOD)
  TAppBOMChildKaynakTakvimKart = class; // (Kaynak Takvimi) (T:URTKYT) (I:VARDIYAKOD;KAYNAKKOD;TARIH;BASTARIHSAAT)

  TAppBOMObjectChild = class
  private
    FOwner: TAppDataControllerBOMObject;
    function GetConnection: TAppConnection;
    function GetCompanyNo: String;
  protected
    procedure CreateObjects; virtual; abstract;
    procedure FreeObjects; virtual; abstract;
    procedure DisableControls; virtual; abstract;
    procedure EnableControls; virtual; abstract;
    function GetIndexFieldNames: String; virtual;
  public
    constructor Create(AOwner: TAppDataControllerBOMObject);
    destructor Destroy; override;
    property Connection: TAppConnection read GetConnection;
    property CompanyNo: String read GetCompanyNo;
  published
    property IndexFieldNames: String read GetIndexFieldNames;
  end;

  // Stok Kart
  TAppBOMChildStokKart = class(TAppBOMObjectChild)
  private
    FTable: TTableStokKart;
    FTableClone: TTableStokKart; // STKKRT
  protected
    procedure CreateObjects; override;
    procedure FreeObjects; override;
    procedure DisableControls; override;
    procedure EnableControls; override;
    function GetIndexFieldNames: String; override; // MALKOD
  public
    procedure Open;
    procedure Close;
    function Find(AMalKod: String): Boolean;
  published
    property Table: TTableStokKart read FTable;
    property TableClone: TTableStokKart read FTableClone;
  end;

  // Stok Kart
  TAppBOMChildStokKartVersiyon = class(TAppBOMObjectChild)
  private
    FTable: TTableStokVersiyon; // STKVER
  protected
    procedure CreateObjects; override;
    procedure FreeObjects; override;
    procedure DisableControls; override;
    procedure EnableControls; override;
    function GetIndexFieldNames: String; override; // MALKOD;VERSIYONNO
  public
    procedure Open;
    procedure Close;
    function Find(AMalKod, AVersiyonNo: String): Boolean;
  published
    property Table: TTableStokVersiyon read FTable;
  end;

  // Stok Kart Birim Çevrim
  TAppBOMChildStokKartBirim = class(TAppBOMObjectChild)
  private
    FTable: TTableStokKartBirim; // STKBRM
  protected
    procedure CreateObjects; override;
    procedure FreeObjects; override;
    procedure DisableControls; override;
    procedure EnableControls; override;
    function GetIndexFieldNames: String; override; // MALKOD;BIRIM
  public
    procedure Open;
    procedure Close;
    function Find(AMalKod, ABirim: String): Boolean;
  published
    property Table: TTableStokKartBirim read FTable;
  end;

  // Stok Kart Alternatif
  TAppBOMChildStokKartAlternatif = class(TAppBOMObjectChild)
  private
    FTable: TTableStokKartAlternatif;
  protected
    procedure CreateObjects; override;
    procedure FreeObjects; override;
    procedure DisableControls; override;
    procedure EnableControls; override;
    function GetIndexFieldNames: String; override;
  public
    procedure Open;
    procedure Close;
    function Find(AMalKod, AVersiyonNo: String): Boolean;
  published
    property Table: TTableStokKartAlternatif read FTable;
  end;

  // Cari Kart
  TAppBOMChildCariKart = class(TAppBOMObjectChild)
  private
    FTable: TTableCariKart; // CARKRT
  protected
    procedure CreateObjects; override;
    procedure FreeObjects; override;
    procedure DisableControls; override;
    procedure EnableControls; override;
    function GetIndexFieldNames: String; override; // HESAPKOD
  public
    procedure Open;
    procedure Close;
    function Find(AHesapKod: String): Boolean;
  published
    property Table: TTableCariKart read FTable;
  end;

  // Hesap Stok Kart
  TAppBOMChildHesapStokKart = class(TAppBOMObjectChild)
  private
    FTable: TTableHesapStokKart; // STKHMK
  protected
    procedure CreateObjects; override;
    procedure FreeObjects; override;
    procedure DisableControls; override;
    procedure EnableControls; override;
    function GetIndexFieldNames: String; override; // MALKOD;VERSIYONNO;HESAPKOD
  public
    procedure Open;
    procedure Close;
    function Find(AMalKod, AVersiyonNo, AHesapKod: String): Boolean;
    procedure AddDagitimMiktar(AMalKod, AVersiyonNo, AHesapKod: String; AMiktar: Double);
  published
    property Table: TTableHesapStokKart read FTable;
  end;

  // MRP Alan
  TAppBOMChildMRPAlanKart = class(TAppBOMObjectChild)
  private
    FTable: TTableMRPAlanKart; // MRPALN
  protected
    procedure CreateObjects; override;
    procedure FreeObjects; override;
    procedure DisableControls; override;
    procedure EnableControls; override;
    function GetIndexFieldNames: String; override; // MRPALANKOD
  public
    procedure Open;
    procedure Close;
    function Find(AMRPAlanKod: String): Boolean;
  published
    property Table: TTableMRPAlanKart read FTable;
  end;

  // MRPAlan Stok Kart
  TAppBOMChildMRPAlanStokKart = class(TAppBOMObjectChild)
  private
    FTable: TTableMRPAlanStokKart;
    FTableClone: TTableMRPAlanStokKart; // STKMAK
  protected
    procedure CreateObjects; override;
    procedure FreeObjects; override;
    procedure DisableControls; override;
    procedure EnableControls; override;
    function GetIndexFieldNames: String; override; // MALKOD;VERSIYONNO;MRPALANKOD
  public
    procedure Open(FabrikaKod: String);
    procedure Close;
    function Find(AMalKod, AVersiyonNo, AMRPAlanKod: String): Boolean;
  published
    property Table: TTableMRPAlanStokKart read FTable;
    property TableClone: TTableMRPAlanStokKart read FTableClone;
  end;

  // Depo Kart
  TAppBOMChildDepoKart = class(TAppBOMObjectChild)
  private
    FTable: TTableDepoKart; // DEPKRT
  protected
    procedure CreateObjects; override;
    procedure FreeObjects; override;
    procedure DisableControls; override;
    procedure EnableControls; override;
    function GetIndexFieldNames: String; override; // DEPOKOD
  public
    procedure Open;
    procedure Close;
    function Find(ADepoKod: String): Boolean;
  published
    property Table: TTableDepoKart read FTable;
  end;

  // Depo Stok Kart
  TAppBOMChildDepoStokKart = class(TAppBOMObjectChild)
  private
    FTable: TTableDepoStokKart; // STKDMK
  protected
    procedure CreateObjects; override;
    procedure FreeObjects; override;
    procedure DisableControls; override;
    procedure EnableControls; override;
    function GetIndexFieldNames: String; override; // MALKOD;VERSIYONNO;DEPOKOD
  public
    procedure Open;
    procedure Close;
    function Find(AMalKod, AVersiyonNo, ADepoKod: String): Boolean;
  published
    property Table: TTableDepoStokKart read FTable;
  end;

  // MRP Parametre
  TAppBOMChildMRPParametre = class(TAppBOMObjectChild)
  private
    FTable: TTableMRPParametre; // MRPPRM
  protected
    procedure CreateObjects; override;
    procedure FreeObjects; override;
    procedure DisableControls; override;
    procedure EnableControls; override;
    function GetIndexFieldNames: String; override; // 
  public
    procedure Open;
    procedure Close;
  published
    property Table: TTableMRPParametre read FTable;
  end;
  
  // İş Merkezi Kartı
  TAppBOMChildIsMerkezKart = class(TAppBOMObjectChild)
  private
    FTable: TTableIsMerkezKart; // URTISM
  protected
    procedure CreateObjects; override;
    procedure FreeObjects; override;
    procedure DisableControls; override;
    procedure EnableControls; override;
    function GetIndexFieldNames: String; override; // ISMERKEZKOD
  public
    procedure Open;
    function Find(AIsMerkezKod: String): Boolean;
  published
    property Table: TTableIsMerkezKart read FTable;
  end;

  // Kaynak Kartı
  TAppBOMChildKaynakKart = class(TAppBOMObjectChild)
  private
    FTable: TTableUretimKaynakKart; // URTKYN
  protected
    procedure CreateObjects; override;
    procedure FreeObjects; override;
    procedure DisableControls; override;
    procedure EnableControls; override;
    function GetIndexFieldNames: String; override; // KAYNAKKOD
  public
    procedure Open;
    function Find(AKaynakKod: String): Boolean;
  published
    property Table: TTableUretimKaynakKart read FTable;
  end;

  // İş Merkezlerinde bulunan Kaynaklar
  TAppBOMChildIsMerkezKaynakKart = class(TAppBOMObjectChild)
  private
    FTable: TTableIsMerkezKaynak; // URTISK
  protected
    procedure CreateObjects; override;
    procedure FreeObjects; override;
    procedure DisableControls; override;
    procedure EnableControls; override;
    function GetIndexFieldNames: String; override; // ISMERKEZKOD;KULLANIMSIRANO
  public
    procedure Open;
    procedure Range(AIsMerkezKod: String);
  published
    property Table: TTableIsMerkezKaynak read FTable;
  end;

  // Mamül Başlık
  TAppBOMChildMamulBaslik = class(TAppBOMObjectChild)
  private
    FTable: TTableMamulBaslik; // MAMBAS
  protected
    procedure CreateObjects; override;
    procedure FreeObjects; override;
    procedure DisableControls; override;
    procedure EnableControls; override;
    function GetIndexFieldNames: String; override; // MAMULKOD;VERSIYONNO;SURUMNO
  public
    procedure Open;
    procedure Close;
    function Find(AMalKod: String; ASurumNo: Integer; AVersiyonNo: String): Boolean;
  published
    property Table: TTableMamulBaslik read FTable;
  end;

  // Mamül kart
  TAppBOMChildMamulKart = class(TAppBOMObjectChild)
  private
    FOpenedAll: Boolean;
    FTopluIslem: Boolean;
    FTableKullanimGrup: TTableMamulKullanimGrup;
    FTableBaslik: TTableMamulBaslik;
    FTableRevizyon: TTableMamulRevizyon;
    FTableKart: TTableMamulKart;
    FTableYanUrun: TTableMamulYanUrun;
    FTableRota: TTableMamulRota;
    FTableRotaSonrakiOperasyon: TTableMamulRotaSonrakiOperasyon;
    FTableRotaKaynak: TTableMamulRotaKaynak;
  protected
    procedure CreateObjects; override;
    procedure FreeObjects; override;
    procedure DisableControls; override;
    procedure EnableControls; override;
  public
    procedure OpenAll;
    procedure Open(AKullanimGrupNo: Smallint; AMamulKod, AMamulVersiyonNo, AMamulKullanimKod: String; AMamulSurumNo: Integer; ATarih: TDateTime; var AReceteNo: String; var ARevizyonNo: String);
    procedure OpenReverse(AHammaddeKod, AHammaddeVersiyonNo: String); // Kullanım Ağacı
    procedure Close;
  published
    property TableKullanimGrup: TTableMamulKullanimGrup read FTableKullanimGrup;
    property TableBaslik: TTableMamulBaslik read FTableBaslik;
    property TableRevizyon: TTableMamulRevizyon read FTableRevizyon;
    property TableKart: TTableMamulKart read FTableKart;
    property TableYanUrun: TTableMamulYanUrun read FTableYanUrun;
    property TableRota: TTableMamulRota read FTableRota;
    property TableRotaSonrakiOperasyon: TTableMamulRotaSonrakiOperasyon read FTableRotaSonrakiOperasyon;
    property TableRotaKaynak: TTableMamulRotaKaynak read FTableRotaKaynak;

    // Parameters
    property TopluIslem: Boolean read FTopluIslem write FTopluIslem;
  end;

  // Özel Mamül kart
  TAppBOMChildOzelMamulKart = class(TAppBOMObjectChild)
  private
    FTable: TTableOzelMamulKart; // STHOMK
    procedure SetDefinitions;
  protected
    procedure CreateObjects; override;
    procedure FreeObjects; override;
    procedure DisableControls; override;
    procedure EnableControls; override;
    function GetIndexFieldNames: String; override; // SIRANO
  public
    procedure Open(AEvrakTip: Smallint; AHesapKod, AEvrakNo: String; ASiraNo: Integer; AMamulKod, AMamulVersiyonNo: String; AMamulSurumNo: Integer);
    procedure Close;
  published
    property Table: TTableOzelMamulKart read FTable;
  end;

  // Mamüllerin Rotaları
  TAppBOMChildMamulRotaKart = class(TAppBOMObjectChild)
  private
    FTable: TTableMamulRota; // MAMROT
  protected
    procedure CreateObjects; override;
    procedure FreeObjects; override;
    procedure DisableControls; override;
    procedure EnableControls; override;
    function GetIndexFieldNames: String; override; // OPERASYONNO
  public
    function Open(AMamulKod: String; ASurumNo: Integer; AVersiyonNo: String): Boolean;
  published
    property Table: TTableMamulRota read FTable;
  end;

  // Mamüle özel rota kaynak ları tanımlı ise bu tabloya bakılıyor.
  TAppBOMChildMamulRotaKaynakKart = class(TAppBOMObjectChild)
  private
    FTable: TTableMamulRotaKaynak; // MAMKYN
  protected
    procedure CreateObjects; override;
    procedure FreeObjects; override;
    procedure DisableControls; override;
    procedure EnableControls; override;
    function GetIndexFieldNames: String; override; // KULLANIMSIRANO
  public
    function Open(AMamulKod: String; ASurumNo: Integer; AVersiyonNo: String): Boolean; overload;
    function Open(AMamulKod: String; ASurumNo: Integer; AVersiyonNo: String; AOperasyonNo: Integer): Boolean; overload;
  published
    property Table: TTableMamulRotaKaynak read FTable;
  end;

  // Kaynak İşlem Tanımları
  TAppBOMChildKaynakIslemTanim = class(TAppBOMObjectChild)
  private
    FTable: TTableUretimKaynakIslemTanim; // URTKIT
  protected
    procedure CreateObjects; override;
    procedure FreeObjects; override;
    procedure DisableControls; override;
    procedure EnableControls; override;
    function GetIndexFieldNames: String; override; // ISLEMNO
  public
    function Open(AKaynakKod: String): Boolean;
  published
    property Table: TTableUretimKaynakIslemTanim read FTable;
  end;

  // Mamüllerin Kaynaklardaki değişim sürelerinin tutulduğu kart
  TAppBOMChildKaynakMamulDegisim = class(TAppBOMObjectChild)
  private
    FTable: TTableUretimKaynakMamulDegisim; // URTKMD
  protected
    procedure CreateObjects; override;
    procedure FreeObjects; override;
    procedure DisableControls; override;
    procedure EnableControls; override;
    function GetIndexFieldNames: String; override; // KURULUMMAMULKOD;SOKUMMAMULKOD
  public
    function Open(AKaynakKod, AKurulumMamulKod, ASokumMamulKod: String): Boolean;
    function Find(AKaynakKod, AKurulumMamulKod, ASokumMamulKod: String): Boolean;
  published
    property Table: TTableUretimKaynakMamulDegisim read FTable;
  end;

  // Mamüllerin Kaynaklardaki değişim sürelerinin tutulduğu kart
  TAppBOMChildKaynakMamulKurulum = class(TAppBOMObjectChild)
  private
    FTable: TTableUretimKaynakMamulKurulum; // URTKMK
  protected
    procedure CreateObjects; override;
    procedure FreeObjects; override;
    procedure DisableControls; override;
    procedure EnableControls; override;
    function GetIndexFieldNames: String; override; // KAYNAKKOD;MAMULKOD
    function Open(AKaynakKod, AMamulKod: String): Boolean;
  public
    function Find(AKaynakKod, AMamulKod: String): Boolean;
  published
    property Table: TTableUretimKaynakMamulKurulum read FTable;
  end;

  TAppBOMChildTakvimVardiyaKart = class(TAppBOMObjectChild)
  private
    FTable: TTableUretimTakvimVardiya; // URTTKV
  protected
    procedure CreateObjects; override;
    procedure FreeObjects; override;
    procedure DisableControls; override;
    procedure EnableControls; override;
    function GetIndexFieldNames: String; override; // TAKVIMKOD
  public
    procedure Open;
    procedure Range(ATakvimKod: String);
  published
    property Table: TTableUretimTakvimVardiya read FTable;
  end;

  TAppBOMChildKaynakTakvimKart = class(TAppBOMObjectChild)
  private
    FTableIn: TTableUretimKaynakTakvim; // URTKYT
    FTable: TTableUretimKaynakTakvim;
    procedure Open(AVardiyaKod, AKaynakKod: String; ATarih: TDateTime; const ABitTarih: TDateTime = 0);
  protected
    procedure CreateObjects; override;
    procedure FreeObjects; override;
    procedure DisableControls; override;
    procedure EnableControls; override;
    function GetIndexFieldNames: String; override; // VARDIYAKOD;KAYNAKKOD;TARIH;BASTARIHSAAT
  public
    procedure Append(AVardiyaKod, AKaynakKod: String; ABasTarih, ABitTarih: TDateTime);
    procedure Range(AVardiyaKod, AKaynakKod: String; ATarih: TDateTime); overload;
    procedure Range(AVardiyaKod, AKaynakKod: String; ABasTarih, ABitTarih: TDateTime); overload;
    function GetKapasite(AVardiyaKod, AKaynakKod: String; ABasTarih, ABitTarih: TDateTime): Double;
    procedure Reset;
    procedure IncKullanilan(AKullanilanSure: Double);
    function IsEmpty(AVardiyaKod, AKaynakKod: String; ATarih: TDateTime): Boolean;
    function FirstEmpty(AVardiyaKod, AKaynakKod: String; ATarih: TDateTime): TDateTime;
  published
    property Table: TTableUretimKaynakTakvim read FTable;
  end;

  TAppDataControllerBOMObject = class
  private
    // Childs
    FChildList: TList;
    FStokKart: TAppBOMChildStokKart;
    FStokKartVersiyon: TAppBOMChildStokKartVersiyon;
    FStokKartBirim: TAppBOMChildStokKartBirim;
    FStokKartAlternatif: TAppBOMChildStokKartAlternatif;
    FCariKart: TAppBOMChildCariKart;
    FHesapStokKart: TAppBOMChildHesapStokKart;
    FMRPAlanKart: TAppBOMChildMRPAlanKart;
    FMRPAlanStokKart: TAppBOMChildMRPAlanStokKart;
    FDepoKart: TAppBOMChildDepoKart;
    FDepoStokKart: TAppBOMChildDepoStokKart;
    FMRPParametre: TAppBOMChildMRPParametre;
    FIsMerkezKart: TAppBOMChildIsMerkezKart;
    FKaynakKart: TAppBOMChildKaynakKart;
    FIsMerkezKaynakKart: TAppBOMChildIsMerkezKaynakKart;
    FMamulBaslik: TAppBOMChildMamulBaslik;
    FMamulKart: TAppBOMChildMamulKart;
    FOzelMamulKart: TAppBOMChildOzelMamulKart;
    FMamulRotaKart: TAppBOMChildMamulRotaKart;
    FMamulRotaKaynakKart: TAppBOMChildMamulRotaKaynakKart;
    FKaynakIslem: TAppBOMChildKaynakIslemTanim;
    FMamulDegisim: TAppBOMChildKaynakMamulDegisim;
    FMamulKurulum: TAppBOMChildKaynakMamulKurulum;
    FKaynakTakvimKart: TAppBOMChildKaynakTakvimKart;
    FTakvimVardiyaKart: TAppBOMChildTakvimVardiyaKart;
    // procedures
    procedure AddChild(AChild: TAppBOMObjectChild);
    procedure Remove(AChild: TAppBOMObjectChild);
    // functions
    function GetCompanyNo: String;
    function GetConnection: TAppConnection;
  public
    constructor Create;
    destructor Destroy; override;
    procedure DisableControls;
    procedure EnableControls;
  published
    property StokKart: TAppBOMChildStokKart read FStokKart;
    property StokKartVersiyon: TAppBOMChildStokKartVersiyon read FStokKartVersiyon;
    property StokKartBirim: TAppBOMChildStokKartBirim read FStokKartBirim;
    property StokKartAlternatif: TAppBOMChildStokKartAlternatif read FStokKartAlternatif;
    property CariKart: TAppBOMChildCariKart read FCariKart;
    property HesapStokKart: TAppBOMChildHesapStokKart read FHesapStokKart;
    property MRPAlanKart: TAppBOMChildMRPAlanKart read FMRPAlanKart;
    property MRPAlanStokKart: TAppBOMChildMRPAlanStokKart read FMRPAlanStokKart;
    property DepoKart: TAppBOMChildDepoKart read FDepoKart;
    property DepoStokKart: TAppBOMChildDepoStokKart read FDepoStokKart;
    property MRPParametre: TAppBOMChildMRPParametre read FMRPParametre;

    property IsMerkezKart: TAppBOMChildIsMerkezKart read FIsMerkezKart;
    property KaynakKart: TAppBOMChildKaynakKart read FKaynakKart;
    property IsMerkezKaynakKart: TAppBOMChildIsMerkezKaynakKart read FIsMerkezKaynakKart;
    property MamulBaslik: TAppBOMChildMamulBaslik read FMamulBaslik;
    property MamulKart: TAppBOMChildMamulKart read FMamulKart;
    property OzelMamulKart: TAppBOMChildOzelMamulKart read FOzelMamulKart;
    property MamulRotaKart: TAppBOMChildMamulRotaKart read FMamulRotaKart;
    property MamulRotaKaynakKart: TAppBOMChildMamulRotaKaynakKart read FMamulRotaKaynakKart;
    property TakvimVardiyaKart: TAppBOMChildTakvimVardiyaKart read FTakvimVardiyaKart;
    property KaynakIslem: TAppBOMChildKaynakIslemTanim read FKaynakIslem;
    property MamulDegisim: TAppBOMChildKaynakMamulDegisim read FMamulDegisim;
    property MamulKurulum: TAppBOMChildKaynakMamulKurulum read FMamulKurulum;
    property KaynakTakvimKart: TAppBOMChildKaynakTakvimKart read FKaynakTakvimKart;

    property Connection: TAppConnection read GetConnection;
    property CompanyNo: String read GetCompanyNo;
  end;

implementation

uses CPMApp_Security, CPMApp_Date;

{ TAppBOMObjectChild }

constructor TAppBOMObjectChild.Create(AOwner: TAppDataControllerBOMObject);
begin
  inherited Create;
  FOwner := AOwner;
  if Assigned(AOwner) then
    FOwner.AddChild(Self);
  CreateObjects;
end;

destructor TAppBOMObjectChild.Destroy;
begin
  FreeObjects;
  if Assigned(FOwner) then
    FOwner.Remove(Self);
  inherited;
end;

function TAppBOMObjectChild.GetCompanyNo: String;
begin
  Result := AppSecurity.DBCompanyNo;
end;

function TAppBOMObjectChild.GetConnection: TAppConnection;
begin
  Result := AppSecurity.ConnectionApp;
end;

function TAppBOMObjectChild.GetIndexFieldNames: String;
begin

end;

{ TAppBOMChildStokKart }

procedure TAppBOMChildStokKart.Close;
begin
  FTable.Close;
end;

procedure TAppBOMChildStokKart.CreateObjects;
begin
  // Create Table
  FTable := TTableStokKart.Create(nil);
  FTable.Connection := Connection;
  FTable.IndexFieldNames := IndexFieldNames; //'MALKOD';

  // Set Definitions
  FTable.TableItems.TableNames := 'STKKRT';

  // Create Table Clone
  FTableClone := TTableStokKart.Create(nil);
  FTableClone.Connection := Connection;
  FTableClone.IndexFieldNames := IndexFieldNames; //'MALKOD';
  FTable.AddClone(FTableClone);
end;

procedure TAppBOMChildStokKart.DisableControls;
begin
  FTable.DisableControls;
end;

procedure TAppBOMChildStokKart.EnableControls;
begin
  FTable.EnableControls;
end;

function TAppBOMChildStokKart.Find(AMalKod: String): Boolean;
begin
  Result := FTable.FindKey([AMalKod]);
end;

procedure TAppBOMChildStokKart.FreeObjects;
begin
  FreeAndNil(FTableClone);
  FreeAndNil(FTable);
end;

function TAppBOMChildStokKart.GetIndexFieldNames: String;
begin
  Result := 'MALKOD';
end;

procedure TAppBOMChildStokKart.Open;
begin
  FTable.Close;
  if FTable.FieldCount = 0 then
  begin
    FTable.DoInitializeRecord;
    FTable.Close;
  end;
  FTable.Open;
end;

{ TAppBOMChildStokKartBirim }

procedure TAppBOMChildStokKartBirim.Close;
begin
  FTable.Close;
end;

procedure TAppBOMChildStokKartBirim.CreateObjects;
begin
  FTable := TTableStokKartBirim.Create(nil);
  FTable.Connection := Connection;
  FTable.ReadOnly := True;
  FTable.IndexFieldNames := IndexFieldNames; //'MALKOD;HESAPKOD';
  FTable.TableItems.TableNames := 'STKBRM';
end;

procedure TAppBOMChildStokKartBirim.DisableControls;
begin
  FTable.DisableControls;
end;

procedure TAppBOMChildStokKartBirim.EnableControls;
begin
  FTable.EnableControls;
end;

function TAppBOMChildStokKartBirim.Find(AMalKod, ABirim: String): Boolean;
begin
  Result := FTable.FindKey([AMalKod, ABirim]);
end;

procedure TAppBOMChildStokKartBirim.FreeObjects;
begin
  FreeAndNil(FTable);
end;

function TAppBOMChildStokKartBirim.GetIndexFieldNames: String;
begin
  Result := 'MALKOD;BIRIM';
end;

procedure TAppBOMChildStokKartBirim.Open;
begin
  FTable.Close;
  if FTable.FieldCount = 0 then
  begin
    FTable.DoInitializeRecord;
    FTable.Close;
  end;
  FTable.Open;
end;

{ TAppBOMChildCariKart }

procedure TAppBOMChildCariKart.Close;
begin
  FTable.Close;
end;

procedure TAppBOMChildCariKart.CreateObjects;
begin
  FTable := TTableCariKart.Create(nil);
  FTable.Connection := Connection;
  FTable.ReadOnly := True;
  FTable.IndexFieldNames := IndexFieldNames; //'HESAPKOD';
  FTable.TableItems.TableNames := 'CARKRT';
end;

procedure TAppBOMChildCariKart.DisableControls;
begin
  FTable.DisableControls;
end;

procedure TAppBOMChildCariKart.EnableControls;
begin
  FTable.EnableControls;
end;

function TAppBOMChildCariKart.Find(AHesapKod: String): Boolean;
begin
  Result := FTable.FindKey([AHesapKod]);
end;

procedure TAppBOMChildCariKart.FreeObjects;
begin
  FreeAndNil(FTable);
end;

function TAppBOMChildCariKart.GetIndexFieldNames: String;
begin
  Result := 'HESAPKOD';
end;

procedure TAppBOMChildCariKart.Open;
begin
  FTable.Close;
  if FTable.FieldCount = 0 then
  begin
    FTable.DoInitializeRecord;
    FTable.Close;
  end;
  FTable.Open;
end;

{ TAppBOMChildHesapStokKart }

procedure TAppBOMChildHesapStokKart.AddDagitimMiktar(AMalKod, AVersiyonNo, AHesapKod: String; AMiktar: Double);
begin
  if Find(AMalKod, AVersiyonNo, AHesapKod) then
  begin
    Table.Edit;
    Table.DagitimMiktar := Table.DagitimMiktar + AMiktar;
    Table.DagitimSayi := Table.DagitimSayi + 1;
    Table.Post;
  end;
end;

procedure TAppBOMChildHesapStokKart.Close;
begin
  FTable.Close;
end;

procedure TAppBOMChildHesapStokKart.CreateObjects;
begin
  FTable := TTableHesapStokKart.Create(nil);
  FTable.Connection := Connection;
  FTable.EnableLogChanges := False;
  FTable.IndexFieldNames := IndexFieldNames; //'MALKOD;VERSIYONNO;HESAPKOD';

  // Set Table Definitions
  FTable.TableItems.TableNames := 'STKHMK,STKKRT,CARKRT';
  FTable.TableItems.TableCaptions := 'Hesap Stok Kart,Stok Kart,Cari Kart';
  with FTable.TableItems[0] do
  begin
    with Fields do
    begin
      Clear;
      Add('MALKOD');
      Add('VERSIYONNO');
      Add('HESAPKOD');
      Add('DAGITIMONCELIK');
      Add('DAGITIMYUZDE');
      Add('MINMIKTAR');
      Add('MAXMIKTAR');
      Add('PARTIBUYUKLUK');
      Add('MAXDAGITIMSAYI');
      AddExpression('CAST(0 AS NUMERIC(25,6))', '_DAGITIMMIKTAR');
      AddExpression('CAST(0 AS SMALLINT)', '_DAGITIMSAYI');
      Add('HAZIRLAMASURE');
      Add('SEVKIYATGUN');
      Add('SEVKIYATSURE');
      Add('GUMRUKSURE');
      Add('DEPOKOD');
      Add('HAMMADDEDEPOKOD');
    end;
    with Where do
    begin
      Clear;
      Add('TIP', wcEqual, 1);
    end;
  end;
  with FTable.TableItems[1] do
  begin
    with Fields do
    begin
      Clear;
    end;
    with Join do
    begin
      Clear;
      Add('MALKOD', 'MALKOD');
    end;
    with Where do
    begin
      Clear;
      Add('ALIMDAGITIMTIP', wcGreater, 0);
    end;
  end;
  with FTable.TableItems[2] do
  begin
    with Fields do
    begin
      Clear;
      Add('HESAPTIP');
    end;
    with Join do
    begin
      Clear;
      Add('HESAPKOD', 'HESAPKOD');
    end;
  end;
end;

procedure TAppBOMChildHesapStokKart.DisableControls;
begin
  FTable.DisableControls;
end;

procedure TAppBOMChildHesapStokKart.EnableControls;
begin
  FTable.EnableControls;
end;

function TAppBOMChildHesapStokKart.Find(AMalKod, AVersiyonNo, AHesapKod: String): Boolean;
begin
  Result := FTable.FindKey([AMalKod, AVersiyonNo, AHesapKod]);
end;

procedure TAppBOMChildHesapStokKart.FreeObjects;
begin
  FreeAndNil(FTable);
end;

function TAppBOMChildHesapStokKart.GetIndexFieldNames: String;
begin
  Result := 'MALKOD;VERSIYONNO;HESAPKOD';
end;

procedure TAppBOMChildHesapStokKart.Open;
begin
  FTable.Close;
  if FTable.FieldCount = 0 then
  begin
    FTable.DoInitializeRecord;
    FTable.Close;
  end;
  FTable.Open;
end;

{ TAppBOMChildMRPAlanKart }

procedure TAppBOMChildMRPAlanKart.Close;
begin
  FTable.Close;
end;

procedure TAppBOMChildMRPAlanKart.CreateObjects;
begin
  FTable := TTableMRPAlanKart.Create(nil);
  FTable.Connection := Connection;
  FTable.ReadOnly := True;
  FTable.IndexFieldNames := IndexFieldNames; //'MRPALANKOD';
  FTable.TableItems.TableNames := 'MRPALN';
end;

procedure TAppBOMChildMRPAlanKart.DisableControls;
begin
  FTable.DisableControls;
end;

procedure TAppBOMChildMRPAlanKart.EnableControls;
begin
  FTable.EnableControls;
end;

function TAppBOMChildMRPAlanKart.Find(AMRPAlanKod: String): Boolean;
begin
  Result := FTable.FindKey([AMRPAlanKod]);
end;

procedure TAppBOMChildMRPAlanKart.FreeObjects;
begin
  FreeAndNil(FTable);
end;

function TAppBOMChildMRPAlanKart.GetIndexFieldNames: String;
begin
  Result := 'MRPALANKOD';
end;

procedure TAppBOMChildMRPAlanKart.Open;
begin
  FTable.Close;
  if FTable.FieldCount = 0 then
  begin
    FTable.DoInitializeRecord;
    FTable.Close;
  end;
  FTable.Open;
end;

{ TAppBOMChildMRPAlanStokKart }

procedure TAppBOMChildMRPAlanStokKart.Close;
begin
  FTable.Close;
end;

procedure TAppBOMChildMRPAlanStokKart.CreateObjects;
begin
  // Create Table
  FTable := TTableMRPAlanStokKart.Create(nil);
  FTable.Connection := Connection;
  FTable.EnableLogChanges := False;
  FTable.IndexFieldNames := IndexFieldNames; //'MALKOD;MRPALANKOD';

  // Set Table Definitions
  FTable.TableItems.TableNames := 'STKMAK,MRPALN,MRPSKP';
  with FTable.TableItems[0] do
  begin
    with Fields do
    begin
      Clear;
      Add('*');
    end;
  end;
  with FTable.TableItems[1] do
  begin
    with Fields do
    begin
      Clear;
      Add('FABRIKAKOD');
      Add('MRPALANTIP');
      Add('HESAPKOD');
      Add('GIRISDEPOKOD');
    end;
    with Join do
    begin
      Clear;
      Add('MRPALANKOD', 'MRPALANKOD');
    end;
  end;
  with FTable.TableItems[2] do
  begin
    with Fields do
    begin
      Clear;
      // Net İhtiyaç Hesaplama Alanları
      Add('DONEMTIP');
      Add('DONEMADET');
      Add('DONEMGUNTIP');
      Add('DONEMGUNADET');
      Add('MINSTOKSURE');
      Add('HEDEFSTOKSURE');
      Add('MAXSTOKSURE');
    end;
    with Join do
    begin
      Clear;
      Add('STOKKARSILAMAKOD', 'STOKKARSILAMAKOD');
    end;
  end;

  // Create Table Clone
  FTableClone := TTableMRPAlanStokKart.Create(nil);
  FTableClone.Connection := Connection;
  FTableClone.IndexFieldNames := IndexFieldNames;
  FTable.AddClone(FTableClone);
end;

procedure TAppBOMChildMRPAlanStokKart.DisableControls;
begin
  FTable.DisableControls;
end;

procedure TAppBOMChildMRPAlanStokKart.EnableControls;
begin
  FTable.EnableControls;
end;

function TAppBOMChildMRPAlanStokKart.Find(AMalKod, AVersiyonNo, AMRPAlanKod: String): Boolean;
begin
  Result := FTable.FindKey([AMalKod, AVersiyonNo, AMRPAlanKod]);
end;

procedure TAppBOMChildMRPAlanStokKart.FreeObjects;
begin
  FreeAndNil(FTableClone);
  FreeAndNil(FTable);
end;

function TAppBOMChildMRPAlanStokKart.GetIndexFieldNames: String;
begin
  Result := 'MALKOD;VERSIYONNO;MRPALANKOD';
end;

procedure TAppBOMChildMRPAlanStokKart.Open(FabrikaKod: String);
begin
  FTable.Close;
  with FTable.TableItems[1] do
  begin
    with Where do
    begin
      Clear;
      Add('FABRIKAKOD', wcEqual, FabrikaKod);
    end;
  end;
  if FTable.FieldCount = 0 then
  begin
    FTable.DoInitializeRecord;
    FTable.Close;
  end;
  FTable.Open;
end;

{ TAppBOMChildDepoKart }

procedure TAppBOMChildDepoKart.Close;
begin
  FTable.Close;
end;

procedure TAppBOMChildDepoKart.CreateObjects;
begin
  FTable := TTableDepoKart.Create(nil);
  FTable.Connection := Connection;
  FTable.ReadOnly := True;
  FTable.IndexFieldNames := IndexFieldNames; //'DEPOKOD';
  FTable.TableItems.TableNames := 'DEPKRT';
  with FTable.TableItems[0] do
  begin
    with Fields do
    begin
      Clear;
      Add('DEPOKOD');
      Add('MRPALANKOD');
    end;
  end;
end;

procedure TAppBOMChildDepoKart.DisableControls;
begin
  FTable.DisableControls;
end;

procedure TAppBOMChildDepoKart.EnableControls;
begin
  FTable.EnableControls;
end;

function TAppBOMChildDepoKart.Find(ADepoKod: String): Boolean;
begin
  Result := FTable.FindKey([ADepoKod]);
end;

procedure TAppBOMChildDepoKart.FreeObjects;
begin
  FreeAndNil(FTable);
end;

function TAppBOMChildDepoKart.GetIndexFieldNames: String;
begin
  Result := 'DEPOKOD';
end;

procedure TAppBOMChildDepoKart.Open;
begin
  FTable.Close;
  if FTable.FieldCount = 0 then
  begin
    FTable.DoInitializeRecord;
    FTable.Close;
  end;
  FTable.Open;
end;

{ TAppBOMChildDepoStokKart }

procedure TAppBOMChildDepoStokKart.Close;
begin
  FTable.Close;
end;

procedure TAppBOMChildDepoStokKart.CreateObjects;
begin
  FTable := TTableDepoStokKart.Create(nil);
  FTable.Connection := Connection;
  FTable.EnableLogChanges := False;
  FTable.IndexFieldNames := IndexFieldNames; //'MALKOD;DEPOKOD';

  // Set Table Definitions
  FTable.TableItems.TableNames := 'STKDMK';
  FTable.TableItems.TableCaptions := 'Depo Stok Kart';
  with FTable.TableItems[0] do
  begin
    with Fields do
    begin
      Clear;
      Add('*');
    end;
  end;
end;

procedure TAppBOMChildDepoStokKart.DisableControls;
begin
  FTable.DisableControls;
end;

procedure TAppBOMChildDepoStokKart.EnableControls;
begin
  FTable.EnableControls;
end;

function TAppBOMChildDepoStokKart.Find(AMalKod, AVersiyonNo, ADepoKod: String): Boolean;
begin
  Result := FTable.FindKey([AMalKod, AVersiyonNo, ADepoKod]);
end;

procedure TAppBOMChildDepoStokKart.FreeObjects;
begin
  FreeAndNil(FTable);
end;

function TAppBOMChildDepoStokKart.GetIndexFieldNames: String;
begin
  Result := 'MALKOD;VERSIYONNO;DEPOKOD';
end;

procedure TAppBOMChildDepoStokKart.Open;
begin
  FTable.Close;
  if FTable.FieldCount = 0 then
  begin
    FTable.DoInitializeRecord;
    FTable.Close;
  end;
  FTable.Open;
end;

{ TAppBOMChildMRPParametre }

procedure TAppBOMChildMRPParametre.Close;
begin
  FTable.Close;
end;

procedure TAppBOMChildMRPParametre.CreateObjects;
begin
  FTable := TTableMRPParametre.Create(nil);
  FTable.Connection := Connection;

  // Set Table Definitions
  FTable.TableItems.TableNames := 'MRPPRM';
  with FTable.TableItems[0] do
  begin
    with Fields do
    begin
      Clear;
      Add('*');
    end;
  end;
end;

procedure TAppBOMChildMRPParametre.DisableControls;
begin
  FTable.DisableControls;
end;

procedure TAppBOMChildMRPParametre.EnableControls;
begin
  FTable.EnableControls;
end;

procedure TAppBOMChildMRPParametre.FreeObjects;
begin
  FreeAndNil(FTable);
end;

function TAppBOMChildMRPParametre.GetIndexFieldNames: String;
begin
  Result := '';
end;

procedure TAppBOMChildMRPParametre.Open;
begin
  FTable.Close;
  with FTable.TableItems[0].Where do
  begin
    Clear;
    Add('SIRKETNO', wcEqual, CompanyNo);
  end;
  if FTable.FieldCount = 0 then
  begin
    FTable.DoInitializeRecord;
    FTable.Close;
  end;
  FTable.Open;
end;

{ TAppBOMChildIsMerkezKart }

procedure TAppBOMChildIsMerkezKart.CreateObjects;
begin
  FTable := TTableIsMerkezKart.Create(nil);
  FTable.Connection := Connection;
  FTable.ReadOnly := True;
  FTable.IndexFieldNames := IndexFieldNames; //'ISMERKEZKOD';
end;

procedure TAppBOMChildIsMerkezKart.DisableControls;
begin
  FTable.DisableControls;
end;

procedure TAppBOMChildIsMerkezKart.EnableControls;
begin
  FTable.EnableControls;
end;

function TAppBOMChildIsMerkezKart.Find(AIsMerkezKod: String): Boolean;
begin
  if not FTable.Active then
    Open;
  Result := FTable.FindKey([AIsMerkezKod]);
end;

procedure TAppBOMChildIsMerkezKart.FreeObjects;
begin
  FreeAndNil(FTable);
end;

function TAppBOMChildIsMerkezKart.GetIndexFieldNames: String;
begin
  Result := 'ISMERKEZKOD';
end;

procedure TAppBOMChildIsMerkezKart.Open;
begin
  if FTable.Active then
    exit;
  FTable.Open;
end;

{ TAppBOMChildKaynakKart }

procedure TAppBOMChildKaynakKart.CreateObjects;
begin
  FTable := TTableUretimKaynakKart.Create(nil);
  FTable.Connection := Connection;
  FTable.ReadOnly := True;
  FTable.IndexFieldNames := IndexFieldNames;
end;

procedure TAppBOMChildKaynakKart.DisableControls;
begin
  FTable.DisableControls;
end;

procedure TAppBOMChildKaynakKart.EnableControls;
begin
  FTable.EnableControls;
end;

function TAppBOMChildKaynakKart.Find(AKaynakKod: String): Boolean;
begin
  if not FTable.Active then
    Open;
  Result := FTable.FindKey([AKaynakKod]);
end;

procedure TAppBOMChildKaynakKart.FreeObjects;
begin
  FreeAndNil(FTable);
end;

function TAppBOMChildKaynakKart.GetIndexFieldNames: String;
begin
  Result := 'KAYNAKKOD';
end;

procedure TAppBOMChildKaynakKart.Open;
begin
  if Table.Active then
    exit;
  FTable.Open;
end;

{ TAppBOMChildIsMerkezKaynakKart }

procedure TAppBOMChildIsMerkezKaynakKart.CreateObjects;
begin
  FTable := TTableIsMerkezKaynak.Create(nil);
  FTable.Connection := Connection;
  FTable.ReadOnly := True;
  FTable.IndexFieldNames := IndexFieldNames;
end;

procedure TAppBOMChildIsMerkezKaynakKart.DisableControls;
begin
  FTable.DisableControls;
end;

procedure TAppBOMChildIsMerkezKaynakKart.EnableControls;
begin
  FTable.EnableControls;
end;

procedure TAppBOMChildIsMerkezKaynakKart.FreeObjects;
begin
  FreeAndNil(FTable);
end;

function TAppBOMChildIsMerkezKaynakKart.GetIndexFieldNames: String;
begin
  Result := 'ISMERKEZKOD;KULLANIMSIRANO';
end;

procedure TAppBOMChildIsMerkezKaynakKart.Open;
begin
  if FTable.Active then
    exit;
  FTable.Open;
end;

procedure TAppBOMChildIsMerkezKaynakKart.Range(AIsMerkezKod: String);
begin
  if not FTable.Active then
    Open;
  FTable.SetRange([AIsMerkezKod], [AIsMerkezKod]);
end;

{ TAppBOMChildMamulBaslik }

procedure TAppBOMChildMamulBaslik.Close;
begin
  FTable.Close;
end;

procedure TAppBOMChildMamulBaslik.CreateObjects;
begin
  FTable := TTableMamulBaslik.Create(nil);
  FTable.Connection := Connection;
  FTable.ReadOnly := True;
  FTable.IndexFieldNames := IndexFieldNames;
  FTable.TableItems.TableNames := 'MAMBAS';
end;

procedure TAppBOMChildMamulBaslik.DisableControls;
begin
  FTable.DisableControls;
end;

procedure TAppBOMChildMamulBaslik.EnableControls;
begin
  FTable.EnableControls;
end;

function TAppBOMChildMamulBaslik.Find(AMalKod: String; ASurumNo: Integer; AVersiyonNo: String): Boolean;
begin
  Result := FTable.FindKey([AMalKod, AVersiyonNo, ASurumNo]);
end;

procedure TAppBOMChildMamulBaslik.FreeObjects;
begin
  FreeAndNil(FTable);
end;

function TAppBOMChildMamulBaslik.GetIndexFieldNames: String;
begin
  Result := 'MAMULKOD;VERSIYONNO;SURUMNO';
end;

procedure TAppBOMChildMamulBaslik.Open;
begin
  FTable.Close;
  with FTable.TableItems[0].Where do
  begin
    Clear;
    Add('SIRKETNO', wcEqual, CompanyNo);
  end;
  if FTable.FieldCount = 0 then
  begin
    FTable.DoInitializeRecord;
    FTable.Close;
  end;
  FTable.Open;
end;

{ TAppBOMChildMamulKart }

procedure TAppBOMChildMamulKart.Close;
begin
  FTableRotaKaynak.Close;
  FTableRotaSonrakiOperasyon.Close;
  FTableRota.Close;
  FTableKart.Close;
  FTableYanUrun.Close;
  FTableRevizyon.Close;
  FTableBaslik.Close;
  FTableKullanimGrup.Close;
end;

procedure TAppBOMChildMamulKart.CreateObjects;

  procedure CreateMamulKullanimGrup;
  begin
    FTableKullanimGrup := TTableMamulKullanimGrup.Create(nil);
    FTableKullanimGrup.Connection := Connection;
    FTableKullanimGrup.ReadOnly := True;
    FTableKullanimGrup.IndexFieldNames := 'GRUPNO;SIRANO';
    FTableKullanimGrup.TableItems.TableNames := VarArrayOf(['MAMKUG']);
    with FTableKullanimGrup.TableItems[0] do // MAMKUG --> Mamül Kullanım Grubu Tablosu
    begin
      with Fields do
      begin
        Clear;
        Add('GRUPNO');
        Add('SIRANO');
        Add('KULLANIMKOD');
      end;
      with Where do
      begin
        Clear;
        Add('SIRKETNO', wcEqual, CompanyNo);
        AddOperator(woAnd);
        Add('KAYITTUR', wcEqual, 1);
        AddOperator(woAnd);
        Add('KAYITDURUM', wcEqual, 1);
      end;
    end;
  end;

  procedure CreateMamulBaslik;
  begin
    FTableBaslik := TTableMamulBaslik.Create(nil);
    FTableBaslik.Connection := Connection;
    FTableBaslik.ReadOnly := True;
    FTableBaslik.IndexFieldNames := 'MAMULKOD;VERSIYONNO;KULLANIMKOD;SURUMNO';
    FTableBaslik.TableItems.TableNames := VarArrayOf(['MAMBAS']);
    with FTableBaslik.TableItems[0] do // MAMBAS --> Mamül Başlık Tablosu
    begin
      with Fields do
      begin
        Clear;
        Add('*');
      end;
    end;
  end;

  procedure CreateMamulRevizyon;
  begin
    FTableRevizyon := TTableMamulRevizyon.Create(nil);
    FTableRevizyon.Connection := Connection;
    FTableRevizyon.ReadOnly := True;
    FTableRevizyon.IndexFieldNames := 'RECETENO;REVIZYONNO;BASTARIH';
    FTableRevizyon.TableItems.TableNames := VarArrayOf(['MAMREV']);
    with FTableRevizyon.TableItems[0] do // MAMBAS --> Mamül Başlık Tablosu
    begin
      with Fields do
      begin
        Clear;
        Add('*');
      end;
    end;
  end;

  procedure CreateMamulKart;
  begin
    FTableKart := TTableMamulKart.Create(nil);
    FTableKart.Connection := Connection;
    FTableKart.ReadOnly := True;
    FTableKart.IndexFieldNames := 'RECETENO;REVIZYONNO;SIRANO;ID';
    FTableKart.TableItems.TableNames := VarArrayOf(['MAMKRT', 'MAMBAS', 'STKKRT', 'STKBRM', 'STKKRT', 'STKBRM']);
    FTableKart.TableItems.TableAlias := VarArrayOf(['MAMKRT', 'MAMBAS', 'MAMSTK', 'MAMBRM', 'STKKRT', 'STKBRM']);
    FTableKart.TableItems.TableReferans := VarArrayOf(['MAMKRT', 'MAMBAS', 'STKKRT', 'STKBRM', 'STKKRT', 'STKBRM']);
    FTableKart.TableItems.TableCaptions := VarArrayOf(['Mamül Kart', 'Mamül Başlık', 'Mamül', 'Mamül Birim', 'Hammadde', 'Hammadde Birim']);
    with FTableKart.TableItems[0] do // MAMKRT --> Mamül Kartı Tablosu
    begin
      with Fields do
      begin
        Clear;
        Add('*');
      end;
    end;
    with FTableKart.TableItems[1] do // MAMBAS --> Mamül Başlığı Tablosu
    begin
      with Fields do
      begin
        Clear;
        Add('BIRIM');
        Add('MAMULDEPOKOD');
        Add('HAMMADDEDEPOKOD');
      end;
      with Join do
      begin
        Clear;
        Add('SIRKETNO', 'SIRKETNO');
        Add('RECETENO', 'RECETENO');
      end;
    end;
    with FTableKart.TableItems[2] do // MAMSTK --> Mamül Stok Kartı Tablosu (In Real STKKRT)
    begin
      with Fields do
      begin
        Clear;
        Add('BIRIM');
        Add('YUVARLAMA');
        Add('MONTAJFIREORAN');
      end;
      with Join do
      begin
        Clear;
        Add('MALKOD', 'MAMULKOD', FTableKart.TableItems[1]);
      end;
    end;
    with FTableKart.TableItems[3] do // MAMBRM --> Mamül Stok Birimleri Tablosu (In Real STKBRM)
    begin
      with Fields do
      begin
        Clear;
        AddExpression('ISNULL(MAMBRM.KATSAYI, -1)', 'MAMBRM_KATSAYI');
      end;
      with Join do
      begin
        Clear;
        Add('MALKOD', 'MAMULKOD', FTableKart.TableItems[1]);
        Add('BIRIM', 'BIRIM', FTableKart.TableItems[1]);
      end;
    end;
    with FTableKart.TableItems[4] do // STKKRT --> Hammadde Stok Kartı Tablosu
    begin
      with Fields do
      begin
        Clear;
        Add('BIRIM');
        Add('MRPTIP');
        Add('TEMINTIP');
        Add('TEMINYONTEM');
        Add('BILESENFIREORAN');
        Add('YUVARLAMA');
        Add('GRUPNO');
      end;
      with Join do
      begin
        Clear;
        Add('MALKOD', 'HAMMADDEKOD');
      end;
    end;
    with FTableKart.TableItems[5] do // STKBRM --> Hammadde Stok Birim Tablosu
    begin
      with Fields do
      begin
        Clear;
        AddExpression('ISNULL(STKBRM.KATSAYI, -1)', 'STKBRM_KATSAYI');
      end;
      with Join do
      begin
        Clear;
        Add('MALKOD', 'HAMMADDEKOD');
        Add('BIRIM', 'HAMMADDEBIRIM');
      end;
    end;
  end;
  procedure CreateMamulYanUrun;
  begin
    FTableYanUrun := TTableMamulYanUrun.Create(nil);
    FTableYanUrun.Connection := Connection;
    FTableYanUrun.ReadOnly := True;
    FTableYanUrun.IndexFieldNames := 'RECETENO;REVIZYONNO;YANURUNNO';
    FTableYanUrun.TableItems.TableNames := VarArrayOf(['MAMYAN', 'MAMBAS', 'STKKRT', 'STKBRM', 'STKKRT', 'STKBRM']);
    FTableYanUrun.TableItems.TableAlias := VarArrayOf(['MAMYAN', 'MAMBAS', 'MAMSTK', 'MAMBRM', 'STKKRT', 'STKBRM']);
    FTableYanUrun.TableItems.TableReferans := VarArrayOf(['MAMYAN', 'MAMBAS', 'STKKRT', 'STKBRM', 'STKKRT', 'STKBRM']);
    FTableYanUrun.TableItems.TableCaptions := VarArrayOf(['Yan Ürün', 'Mamül Başlık', 'Mamül', 'Mamül Birim', 'Hammadde', 'Hammadde Birim']);
    with FTableYanUrun.TableItems[0] do // MAMYAN --> Mamül Yan Ürün Tablosu
    begin
      with Fields do
      begin
        Clear;
        Add('*');

        AddExpression('CAST(0 AS NUMERIC(25,6))', '_MALIYET');
        AddExpression('CAST(0 AS NUMERIC(25,6))', '_MALIYETGRUP1');
        AddExpression('CAST(0 AS NUMERIC(25,6))', '_MALIYETGRUP2');
        AddExpression('CAST(0 AS NUMERIC(25,6))', '_MALIYETGRUP3');
        AddExpression('CAST(0 AS NUMERIC(25,6))', '_MALIYETGRUP4');
        AddExpression('CAST(0 AS NUMERIC(25,6))', '_MALIYETGRUPDIGER');

        AddExpression('CAST(0 AS NUMERIC(25,6))', '_YERELMALIYET1');
        AddExpression('CAST(0 AS NUMERIC(25,6))', '_YERELMALIYET2');
        AddExpression('CAST(0 AS NUMERIC(25,6))', '_YERELMALIYET3');

        AddExpression('CAST(0 AS NUMERIC(25,6))', '_YERELMALIYET');
        AddExpression('CAST(0 AS NUMERIC(25,6))', '_YERELMALIYETGRUP1');
        AddExpression('CAST(0 AS NUMERIC(25,6))', '_YERELMALIYETGRUP2');
        AddExpression('CAST(0 AS NUMERIC(25,6))', '_YERELMALIYETGRUP3');
        AddExpression('CAST(0 AS NUMERIC(25,6))', '_YERELMALIYETGRUP4');
        AddExpression('CAST(0 AS NUMERIC(25,6))', '_YERELMALIYETGRUPDIGER');
      end;
    end;
    with FTableYanUrun.TableItems[1] do // MAMBAS --> Mamül Başlığı Tablosu
    begin
      with Fields do
      begin
        Clear;
        Add('BIRIM');
        Add('MAMULDEPOKOD');
        Add('HAMMADDEDEPOKOD');
      end;
      with Join do
      begin
        Clear;
        Add('SIRKETNO', 'SIRKETNO');
        Add('RECETENO', 'RECETENO');
      end;
    end;
    with FTableYanUrun.TableItems[2] do // MAMSTK --> Mamül Stok Kartı Tablosu (In Real STKKRT)
    begin
      with Fields do
      begin
        Clear;
        Add('BIRIM');
        Add('YUVARLAMA');
        Add('MONTAJFIREORAN');
      end;
      with Join do
      begin
        Clear;
        Add('MALKOD', 'MAMULKOD', FTableYanUrun.TableItems[1]);
      end;
    end;
    with FTableYanUrun.TableItems[3] do // MAMBRM --> Mamül Stok Birimleri Tablosu (In Real STKBRM)
    begin
      with Fields do
      begin
        Clear;
        AddExpression('ISNULL(MAMBRM.KATSAYI, -1)', 'MAMBRM_KATSAYI');
      end;
      with Join do
      begin
        Clear;
        Add('MALKOD', 'MAMULKOD', FTableYanUrun.TableItems[1]);
        Add('BIRIM', 'BIRIM', FTableYanUrun.TableItems[1]);
      end;
    end;
    with FTableYanUrun.TableItems[4] do // STKKRT --> Hammadde Stok Kartı Tablosu
    begin
      with Fields do
      begin
        Clear;
        Add('BIRIM');
        Add('MRPTIP');
        Add('TEMINTIP');
        Add('TEMINYONTEM');
        Add('BILESENFIREORAN');
        Add('YUVARLAMA');
        Add('GRUPNO');
      end;
      with Join do
      begin
        Clear;
        Add('MALKOD', 'MALKOD');
      end;
    end;
    with FTableYanUrun.TableItems[5] do // STKBRM --> Hammadde Stok Birim Tablosu
    begin
      with Fields do
      begin
        Clear;
        AddExpression('ISNULL(STKBRM.KATSAYI, -1)', 'STKBRM_KATSAYI');
      end;
      with Join do
      begin
        Clear;
        Add('MALKOD', 'MALKOD');
        Add('BIRIM', 'BIRIM');
      end;
    end;
  end;
  procedure CreateMamulRota;
  begin
    FTableRota := TTableMamulRota.Create(nil);
    FTableRota.Connection := Connection;
    FTableRota.ReadOnly := True;
    FTableRota.IndexFieldNames := 'RECETENO;REVIZYONNO;OPERASYONNO';
    FTableRota.TableItems.TableNames := VarArrayOf(['MAMROT']);
    with FTableRota.TableItems[0] do // MAMROT --> Mamül Rota Tablosu
    begin
      with Fields do
      begin
        Clear;
        Add('*');
        AddExpression('CAST('''' AS VARCHAR(30))', '_SONRAKIOPERASYONNO');
      end;
    end;
  end;
  procedure CreateMamulRotaSonrakiOperasyon;
  begin
    FTableRotaSonrakiOperasyon := TTableMamulRotaSonrakiOperasyon.Create(nil);
    FTableRotaSonrakiOperasyon.Connection := Connection;
    FTableRotaSonrakiOperasyon.ReadOnly := True;
    FTableRotaSonrakiOperasyon.IndexFieldNames := 'RECETENO;REVIZYONNO;OPERASYONNO;SONRAKIOPERASYONNO';
    FTableRotaSonrakiOperasyon.TableItems.TableNames := VarArrayOf(['MAMROP']);
    with FTableRotaSonrakiOperasyon.TableItems[0] do // MAMROP --> Mamül RotaSonrakiOperasyon Tablosu
    begin
      with Fields do
      begin
        Clear;
        Add('*');
      end;
    end;
  end;
  procedure CreateMamulRotaKaynak;
  begin
    FTableRotaKaynak := TTableMamulRotaKaynak.Create(nil);
    FTableRotaKaynak.Connection := Connection;
    FTableRotaKaynak.ReadOnly := True;
    FTableRotaKaynak.IndexFieldNames := 'RECETENO;REVIZYONNO;OPERASYONNO;KULLANIMSIRANO';
    FTableRotaKaynak.TableItems.TableNames := VarArrayOf(['MAMKYN', 'URTKYN']);
    with FTableRotaKaynak.TableItems[0] do // MAMKYN --> Mamül Kaynak Tablosu
    begin
      with Fields do
      begin
        Clear;
        Add('*');
        AddExpression('CAST(0 AS NUMERIC(25,6))', '_CALCCALISMASURE');
        AddExpression('CAST(0 AS NUMERIC(25,6))', '_CALCTOPLAMSURE');
      end;
    end;
    with FTableRotaKaynak.TableItems[1] do // URTKYN --> Kaynak Tablosu
    begin
      with Fields do
      begin
        Clear;
        Add('KAYNAKAD');
        Add('KURULUMSURE');
        Add('CALISMASURE');
        Add('SOKUMSURE');
      end;
      with Join do
      begin
        Clear;
        Add('KAYNAKKOD', 'KAYNAKKOD');
      end;
    end;
  end;
begin
  CreateMamulKullanimGrup;
  CreateMamulBaslik;
  CreateMamulRevizyon;
  CreateMamulKart;
  CreateMamulYanUrun;
  CreateMamulRota;
  CreateMamulRotaSonrakiOperasyon;
  CreateMamulRotaKaynak;

  FOpenedAll := False;
end;

procedure TAppBOMChildMamulKart.DisableControls;
begin
  FTableKullanimGrup.DisableControls;
  FTableBaslik.DisableControls;
  FTableRevizyon.DisableControls;
  FTableKart.DisableControls;
  FTableYanUrun.DisableControls;
  FTableRota.DisableControls;
  FTableRotaSonrakiOperasyon.DisableControls;
  FTableRotaKaynak.DisableControls;
end;

procedure TAppBOMChildMamulKart.EnableControls;
begin
  FTableKullanimGrup.EnableControls;
  FTableBaslik.EnableControls;
  FTableRevizyon.EnableControls;
  FTableKart.EnableControls;
  FTableYanUrun.EnableControls;
  FTableRota.EnableControls;
  FTableRotaSonrakiOperasyon.EnableControls;
  FTableRotaKaynak.EnableControls;
end;

procedure TAppBOMChildMamulKart.FreeObjects;
begin
  FreeAndNil(FTableKullanimGrup);
  FreeAndNil(FTableBaslik);
  FreeAndNil(FTableRevizyon);
  FreeAndNil(FTableKart);
  FreeAndNil(FTableYanUrun);
  FreeAndNil(FTableRota);
  FreeAndNil(FTableRotaSonrakiOperasyon);
  FreeAndNil(FTableRotaKaynak);
end;

procedure TAppBOMChildMamulKart.OpenAll;

    procedure OpenKullanimGrup;
    begin
      if not FTableKullanimGrup.Active then
      begin
        if FTableKullanimGrup.FieldCount = 0 then
        begin
          FTableKullanimGrup.DoInitializeRecord;
          FTableKullanimGrup.Close;
        end;
        FTableKullanimGrup.Open;
      end;
    end;
    procedure OpenBaslik;
    begin
      FTableBaslik.Close;
      with FTableBaslik.TableItems[0].Where do
      begin
        Clear;
        Add('SIRKETNO', wcEqual, CompanyNo);
        AddOperator(woAnd);
        Add('KAYITTUR', wcEqual, 1);
        AddOperator(woAnd);
        Add('KAYITDURUM', wcEqual, 1);
      end;
      if FTableBaslik.FieldCount = 0 then
      begin
        FTableBaslik.DoInitializeRecord;
        FTableBaslik.Close;
      end;
      FTableBaslik.Open;
    end;
    procedure OpenRevizyon;
    begin
      FTableRevizyon.Close;
      with FTableRevizyon.TableItems[0].Where do
      begin
        Clear;
        Add('SIRKETNO', wcEqual, CompanyNo);
        AddOperator(woAnd);
        Add('KAYITTUR', wcEqual, 1);
        AddOperator(woAnd);
        Add('KAYITDURUM', wcEqual, 1);
      end;
      if FTableRevizyon.FieldCount = 0 then
      begin
        FTableRevizyon.DoInitializeRecord;
        FTableRevizyon.Close;
      end;
      FTableRevizyon.Open;
    end;
    procedure OpenKart;
    begin
      FTableKart.Close;
      with FTableKart.TableItems[0].Where do
      begin
        Clear;
        Add('SIRKETNO', wcEqual, CompanyNo);
        AddOperator(woAnd);
        Add('KAYITTUR', wcEqual, 1);
        AddOperator(woAnd);
        Add('KAYITDURUM', wcEqual, 1);
      end;
      if FTableKart.FieldCount = 0 then
      begin
        FTableKart.DoInitializeRecord;
        FTableKart.Close;
      end;
      FTableKart.Open;
    end;
    procedure OpenYanUrun;
    begin
      FTableYanUrun.Close;
      with FTableYanUrun.TableItems[0].Where do
      begin
        Clear;
        Add('SIRKETNO', wcEqual, CompanyNo);
        AddOperator(woAnd);
        Add('KAYITTUR', wcEqual, 1);
        AddOperator(woAnd);
        Add('KAYITDURUM', wcEqual, 1);
      end;
      if FTableYanUrun.FieldCount = 0 then
      begin
        FTableYanUrun.DoInitializeRecord;
        FTableYanUrun.Close;
      end;
      FTableYanUrun.Open;
    end;
    procedure OpenRota;
    begin
      FTableRota.Close;
      with FTableRota.TableItems[0].Where do
      begin
        Clear;
        Add('SIRKETNO', wcEqual, CompanyNo);
        AddOperator(woAnd);
        Add('KAYITTUR', wcEqual, 1);
        AddOperator(woAnd);
        Add('KAYITDURUM', wcEqual, 1);
      end;
      if FTableRota.FieldCount = 0 then
      begin
        FTableRota.DoInitializeRecord;
        FTableRota.Close;
      end;
      FTableRota.Open;
    end;
    procedure OpenRotaSonrakiOperasyon;
    begin
      FTableRotaSonrakiOperasyon.Close;
      with FTableRotaSonrakiOperasyon.TableItems[0].Where do
      begin
        Clear;
        Add('SIRKETNO', wcEqual, CompanyNo);
        AddOperator(woAnd);
        Add('KAYITTUR', wcEqual, 1);
        AddOperator(woAnd);
        Add('KAYITDURUM', wcEqual, 1);
      end;
      if FTableRotaSonrakiOperasyon.FieldCount = 0 then
      begin
        FTableRotaSonrakiOperasyon.DoInitializeRecord;
        FTableRotaSonrakiOperasyon.Close;
      end;
      FTableRotaSonrakiOperasyon.Open;
    end;
    procedure OpenRotaKaynak;
    begin
      FTableRotaKaynak.Close;
      with FTableRotaKaynak.TableItems[0].Where do
      begin
        Clear;
        Add('SIRKETNO', wcEqual, CompanyNo);
        AddOperator(woAnd);
        Add('KAYITTUR', wcEqual, 1);
        AddOperator(woAnd);
        Add('KAYITDURUM', wcEqual, 1);
      end;
      if FTableRotaKaynak.FieldCount = 0 then
      begin
        FTableRotaKaynak.DoInitializeRecord;
        FTableRotaKaynak.Close;
      end;
      FTableRotaKaynak.Open;
    end;

begin
  if FOpenedAll and FTableKart.Active then
    Exit;
  OpenKullanimGrup;
  OpenBaslik;
  OpenRevizyon;
  OpenKart;
  OpenYanUrun;
  OpenRota;
  OpenRotaSonrakiOperasyon;
  OpenRotaKaynak;
  FOpenedAll := True;
end;

procedure TAppBOMChildMamulKart.Open(AKullanimGrupNo: Smallint; AMamulKod, AMamulVersiyonNo, AMamulKullanimKod: String; AMamulSurumNo: Integer; ATarih: TDateTime; var AReceteNo: String; var ARevizyonNo: String);

  procedure OpenKullanimGrup;
  begin
    if not FTableKullanimGrup.Active then
    begin
      if FTableKullanimGrup.FieldCount = 0 then
      begin
        FTableKullanimGrup.DoInitializeRecord;
        FTableKullanimGrup.Close;
      end;
      FTableKullanimGrup.Open;
    end;
  end;

  function OpenBaslik(AMamulKullanimKod: String): String;
  begin
    if FOpenedAll then
      FTableBaslik.SetRange([AMamulKod, AMamulVersiyonNo, AMamulKullanimKod, AMamulSurumNo], [AMamulKod, AMamulVersiyonNo, AMamulKullanimKod, AMamulSurumNo])
    else
    begin
      FTableBaslik.Close;
      with FTableBaslik.TableItems[0] do
      begin
        with Where do
        begin
          Clear;
          Add('SIRKETNO', wcEqual, CompanyNo);
          AddOperator(woAnd);
          Add('KAYITTUR', wcEqual, 1);
          AddOperator(woAnd);
          Add('KAYITDURUM', wcEqual, 1);
          AddOperator(woAnd);
          Add('MAMULKOD', wcEqual, AMamulKod);
          AddOperator(woAnd);
          Add('VERSIYONNO', wcEqual, AMamulVersiyonNo);
          AddOperator(woAnd);
          Add('KULLANIMKOD', wcEqual, AMamulKullanimKod);
          AddOperator(woAnd);
          Add('SURUMNO', wcEqual, AMamulSurumNo);
        end;
      end;
      if FTableBaslik.FieldCount = 0 then
      begin
        FTableBaslik.DoInitializeRecord;
        FTableBaslik.Close;
      end;
      FTableBaslik.Open;
    end;
    Result := FTableBaslik.ReceteNo;
  end;

  function FindBaslik: String;
  begin
    if AKullanimGrupNo = 0 then
      Result := OpenBaslik(AMamulKullanimKod)
    else
    begin
      Result := '';
      FTableKullanimGrup.SetRange([AKullanimGrupNo], [AKullanimGrupNo]);
      FTableKullanimGrup.First;
      while not FTableKullanimGrup.Eof do
      begin
        Result := OpenBaslik(FTableKullanimGrup.KullanimKod);
        if Trim(Result) <> '' then
          Break;
        FTableKullanimGrup.Next;
      end;
    end;
  end;

  function OpenRevizyon(AReceteNo: String): String;
  begin
    Result := '';
    if FOpenedAll then
      FTableRevizyon.SetRange([AReceteNo], [AReceteNo])
    else
    begin
      FTableRevizyon.Close;
      with FTableRevizyon.TableItems[0] do
      begin
        with Where do
        begin
          Clear;
          Add('SIRKETNO', wcEqual, CompanyNo);
          AddOperator(woAnd);
          Add('KAYITTUR', wcEqual, 1);
          AddOperator(woAnd);
          Add('KAYITDURUM', wcEqual, 1);
          AddOperator(woAnd);
          Add('RECETENO', wcEqual, AReceteNo);
        end;
      end;
      if FTableRevizyon.FieldCount = 0 then
      begin
        FTableRevizyon.DoInitializeRecord;
        FTableRevizyon.Close;
      end;
      FTableRevizyon.Open;
    end;

    FTableRevizyon.Last;
    while not FTableRevizyon.Bof do
    begin
      if FTableRevizyon.BasTarih <= ATarih then
        if (FTableRevizyon.BitTarih = AppFirstDate) or (FTableRevizyon.BitTarih >= ATarih) then
        begin
          Result := FTableRevizyon.RevizyonNo;
          Break;
        end;
      FTableRevizyon.Prior;
    end;
  end;

  procedure OpenKart(AReceteNo, ARevizyonNo: String);
  begin
    if FOpenedAll then
      FTableKart.SetRange([AReceteNo, ARevizyonNo], [AReceteNo, ARevizyonNo])
    else
    begin
      FTableKart.Close;
      with FTableKart.TableItems[0] do
      begin
        with Where do
        begin
          Clear;
          Add('SIRKETNO', wcEqual, CompanyNo);
          AddOperator(woAnd);
          Add('KAYITTUR', wcEqual, 1);
          AddOperator(woAnd);
          Add('KAYITDURUM', wcEqual, 1);
          AddOperator(woAnd);
          Add('RECETENO', wcEqual, AReceteNo);
          AddOperator(woAnd);
          Add('REVIZYONNO', wcEqual, ARevizyonNo);
        end;
      end;
      if FTableKart.FieldCount = 0 then
      begin
        FTableKart.DoInitializeRecord;
        FTableKart.Close;
      end;
      FTableKart.Open;
    end;
  end;
  procedure OpenYanUrun(AReceteNo, ARevizyonNo: String);
  begin
    if FOpenedAll then
      FTableYanUrun.SetRange([AReceteNo, ARevizyonNo], [AReceteNo, ARevizyonNo])
    else
    begin
      FTableYanUrun.Close;
      with FTableYanUrun.TableItems[0] do
      begin
        with Where do
        begin
          Clear;
          Add('SIRKETNO', wcEqual, CompanyNo);
          AddOperator(woAnd);
          Add('KAYITTUR', wcEqual, 1);
          AddOperator(woAnd);
          Add('KAYITDURUM', wcEqual, 1);
          AddOperator(woAnd);
          Add('RECETENO', wcEqual, AReceteNo);
          AddOperator(woAnd);
          Add('REVIZYONNO', wcEqual, ARevizyonNo);
        end;
      end;
      if FTableYanUrun.FieldCount = 0 then
      begin
        FTableYanUrun.DoInitializeRecord;
        FTableYanUrun.Close;
      end;
      FTableYanUrun.Open;
    end;
  end;
  procedure OpenRota(AReceteNo, ARevizyonNo: String);
  begin
    if FOpenedAll then
      FTableRota.SetRange([AReceteNo, ARevizyonNo], [AReceteNo, ARevizyonNo])
    else
    begin
      FTableRota.Close;
      with FTableRota.TableItems[0] do
      begin
        with Where do
        begin
          Clear;
          Add('SIRKETNO', wcEqual, CompanyNo);
          AddOperator(woAnd);
          Add('KAYITTUR', wcEqual, 1);
          AddOperator(woAnd);
          Add('KAYITDURUM', wcEqual, 1);
          AddOperator(woAnd);
          Add('RECETENO', wcEqual, AReceteNo);
          AddOperator(woAnd);
          Add('REVIZYONNO', wcEqual, ARevizyonNo);
        end;
      end;
      if FTableRota.FieldCount = 0 then
      begin
        FTableRota.DoInitializeRecord;
        FTableRota.Close;
      end;
      FTableRota.Open;
    end;
  end;
  procedure OpenRotaSonrakiOperasyon(AReceteNo, ARevizyonNo: String);
  begin
    if FOpenedAll then
      FTableRotaSonrakiOperasyon.SetRange([AReceteNo, ARevizyonNo], [AReceteNo, ARevizyonNo])
    else
    begin
      FTableRotaSonrakiOperasyon.Close;
      with FTableRotaSonrakiOperasyon.TableItems[0] do
      begin
        with Where do
        begin
          Clear;
          Add('SIRKETNO', wcEqual, CompanyNo);
          AddOperator(woAnd);
          Add('KAYITTUR', wcEqual, 1);
          AddOperator(woAnd);
          Add('KAYITDURUM', wcEqual, 1);
          AddOperator(woAnd);
          Add('RECETENO', wcEqual, AReceteNo);
          AddOperator(woAnd);
          Add('REVIZYONNO', wcEqual, ARevizyonNo);
        end;
      end;
      if FTableRotaSonrakiOperasyon.FieldCount = 0 then
      begin
        FTableRotaSonrakiOperasyon.DoInitializeRecord;
        FTableRotaSonrakiOperasyon.Close;
      end;
      FTableRotaSonrakiOperasyon.Open;
    end;
  end;
  procedure OpenRotaKaynak(AReceteNo, ARevizyonNo: String);
  begin
    if FOpenedAll then
      FTableRotaKaynak.SetRange([AReceteNo, ARevizyonNo], [AReceteNo, ARevizyonNo])
    else
    begin
      FTableRotaKaynak.Close;
      with FTableRotaKaynak.TableItems[0] do
      begin
        with Where do
        begin
          Clear;
          Add('SIRKETNO', wcEqual, CompanyNo);
          AddOperator(woAnd);
          Add('KAYITTUR', wcEqual, 1);
          AddOperator(woAnd);
          Add('KAYITDURUM', wcEqual, 1);
          AddOperator(woAnd);
          Add('RECETENO', wcEqual, AReceteNo);
          AddOperator(woAnd);
          Add('REVIZYONNO', wcEqual, ARevizyonNo);
        end;
      end;
      if FTableRotaKaynak.FieldCount = 0 then
      begin
        FTableRotaKaynak.DoInitializeRecord;
        FTableRotaKaynak.Close;
      end;
      FTableRotaKaynak.Open;
    end;
  end;

begin
  if FTopluIslem then
    OpenAll;
  OpenKullanimGrup;
  AReceteNo := FindBaslik;
  ARevizyonNo := OpenRevizyon(AReceteNo);
  OpenKart(AReceteNo, ARevizyonNo);
  OpenYanUrun(AReceteNo, ARevizyonNo);
  OpenRota(AReceteNo, ARevizyonNo);
  OpenRotaSonrakiOperasyon(AReceteNo, ARevizyonNo);
  OpenRotaKaynak(AReceteNo, ARevizyonNo);
end;

procedure TAppBOMChildMamulKart.OpenReverse(AHammaddeKod, AHammaddeVersiyonNo: String);
begin
  FTableKart.Close;
  with FTableKart.TableItems[0] do
  begin
    with Where do
    begin
      Clear;
      Add('SIRKETNO', wcEqual, CompanyNo);
      AddOperator(woAnd);
      Add('KAYITTUR', wcEqual, 1);
      AddOperator(woAnd);
      Add('KAYITDURUM', wcEqual, 1);
      AddOperator(woAnd);
      Add('HAMMADDEKOD', wcEqual, AHammaddeKod);
      AddOperator(woAnd);
      Add('HAMMADDEVERSIYONNO', wcEqual, AHammaddeVersiyonNo);
    end;
  end;
  if FTableKart.FieldCount = 0 then
  begin
    FTableKart.DoInitializeRecord;
    FTableKart.Close;
  end;
  FTableKart.Open;
end;

{ TAppBOMChildMamulRotaKart }

procedure TAppBOMChildMamulRotaKart.CreateObjects;
begin
  FTable := TTableMamulRota.Create(nil);
  FTable.Connection := Connection;
  FTable.ReadOnly := True;
end;

procedure TAppBOMChildMamulRotaKart.DisableControls;
begin
  FTable.DisableControls;
end;

procedure TAppBOMChildMamulRotaKart.EnableControls;
begin
  FTable.EnableControls;
end;

procedure TAppBOMChildMamulRotaKart.FreeObjects;
begin
  FreeAndNil(FTable);
end;

function TAppBOMChildMamulRotaKart.GetIndexFieldNames: String;
begin
  Result := 'OPERASYONNO';
end;

function TAppBOMChildMamulRotaKart.Open(AMamulKod: String; ASurumNo: Integer; AVersiyonNo: String): Boolean;
begin
  FTable.Close;
  FTable.TableItems.TableNames := 'MAMROT';
  with FTable.TableItems[0] do
  begin
    with Fields do
    begin
      Clear;
      Add('*');
    end;
    with Where do
    begin
      Add('SIRKETNO', wcEqual, CompanyNo);
      AddOperator(woAnd);
      Add('KAYITTUR', wcEqual, 1);
      AddOperator(woAnd);
      Add('KAYITDURUM', wcEqual, 1);
      AddOperator(woAnd);
      Add('MAMULKOD', wcEqual, AMamulKod);
      AddOperator(woAnd);
      Add('VERSIYONNO', wcEqual, AVersiyonNo);
      AddOperator(woAnd);
      Add('SURUMNO', wcEqual, ASurumNo);
    end;
  end;
  if FTable.FieldCount = 0 then
  begin
    FTable.DoInitializeRecord;
    FTable.Close;
  end;
  FTable.Open;
  Result := FTable.RecordCount > 0;
end;

{ TAppBOMChildMamulRotaKaynakKart }

procedure TAppBOMChildMamulRotaKaynakKart.CreateObjects;
begin
  FTable := TTableMamulRotaKaynak.Create(nil);
  FTable.Connection := Connection;
  FTable.ReadOnly := True;
  FTable.IndexFieldNames := IndexFieldNames;
end;

procedure TAppBOMChildMamulRotaKaynakKart.DisableControls;
begin
  FTable.DisableControls;
end;

procedure TAppBOMChildMamulRotaKaynakKart.EnableControls;
begin
  FTable.EnableControls;
end;

procedure TAppBOMChildMamulRotaKaynakKart.FreeObjects;
begin
  FreeAndNil(FTable);
end;

function TAppBOMChildMamulRotaKaynakKart.GetIndexFieldNames: String;
begin
  Result := 'KULLANIMSIRANO';
end;

function TAppBOMChildMamulRotaKaynakKart.Open(AMamulKod: String; ASurumNo: Integer;
  AVersiyonNo: String; AOperasyonNo: Integer): Boolean;
begin
  FTable.Close;
  FTable.TableItems.TableNames := 'MAMKYN';
  with FTable.TableItems[0] do
  begin
    with Fields do
    begin
      Clear;
      Add('*');
    end;
    with Where do
    begin
      Add('SIRKETNO', wcEqual, CompanyNo);
      AddOperator(woAnd);
      Add('KAYITTUR', wcEqual, 1);
      AddOperator(woAnd);
      Add('KAYITDURUM', wcEqual, 1);
      AddOperator(woAnd);
      Add('MAMULKOD', wcEqual, AMamulKod);
      AddOperator(woAnd);
      Add('VERSIYONNO', wcEqual, AVersiyonNo);
      AddOperator(woAnd);
      Add('SURUMNO', wcEqual, ASurumNo);
      AddOperator(woAnd);
      Add('OPERASYONNO', wcEqual, AOperasyonNo);
    end;
  end;
  if FTable.FieldCount = 0 then
  begin
    FTable.DoInitializeRecord;
    FTable.Close;
  end;
  FTable.Open;
  Result := FTable.RecordCount > 0;
end;

function TAppBOMChildMamulRotaKaynakKart.Open(AMamulKod: String; ASurumNo: Integer;
  AVersiyonNo: String): Boolean;
begin
  FTable.Close;
  FTable.TableItems.TableNames := 'MAMKYN';
  with FTable.TableItems[0] do
  begin
    with Fields do
    begin
      Clear;
      Add('*');
    end;
    with Where do
    begin
      Add('SIRKETNO', wcEqual, CompanyNo);
      AddOperator(woAnd);
      Add('KAYITTUR', wcEqual, 1);
      AddOperator(woAnd);
      Add('KAYITDURUM', wcEqual, 1);
      AddOperator(woAnd);
      Add('MAMULKOD', wcEqual, AMamulKod);
      AddOperator(woAnd);
      Add('VERSIYONNO', wcEqual, AVersiyonNo);
      AddOperator(woAnd);
      Add('SURUMNO', wcEqual, ASurumNo);
    end;
  end;
  if FTable.FieldCount = 0 then
  begin
    FTable.DoInitializeRecord;
    FTable.Close;
  end;
  FTable.Open;
  Result := FTable.RecordCount > 0;
end;

{ TAppBOMChildKaynakMamulDegisim }

procedure TAppBOMChildKaynakMamulDegisim.CreateObjects;
begin
  FTable := TTableUretimKaynakMamulDegisim.Create(nil);
  FTable.Connection := Connection;
  FTable.ReadOnly := True;
  FTable.IndexFieldNames := IndexFieldNames;
end;

procedure TAppBOMChildKaynakMamulDegisim.DisableControls;
begin
  FTable.DisableControls;
end;

procedure TAppBOMChildKaynakMamulDegisim.EnableControls;
begin
  FTable.EnableControls;
end;

function TAppBOMChildKaynakMamulDegisim.Find(AKaynakKod, AKurulumMamulKod,
  ASokumMamulKod: String): Boolean;
begin
  Result := Open(AKaynakKod, AKurulumMamulKod, ASokumMamulKod);
end;

procedure TAppBOMChildKaynakMamulDegisim.FreeObjects;
begin
  FreeAndNil(FTable);
end;

function TAppBOMChildKaynakMamulDegisim.GetIndexFieldNames: String;
begin
  Result := 'KURULUMMAMULKOD;SOKUMMAMULKOD';
end;

function TAppBOMChildKaynakMamulDegisim.Open(AKaynakKod, AKurulumMamulKod,
  ASokumMamulKod: String): Boolean;
begin
  FTable.Close;
  with FTable.TableItems[0] do // URTKMD
  begin
    with Where do
    begin
      Clear;
      Add('KAYNAKKOD', wcEqual, AKaynakKod);
      AddOperator(woAnd);
      Add('KURULUMMAMULKOD', wcEqual, AKurulumMamulKod);
      AddOperator(woAnd);
      Add('SOKUMMAMULKOD', wcEqual, ASokumMamulKod);
    end;
  end;
  if FTable.FieldCount = 0 then
  begin
    FTable.DoInitializeRecord;
    FTable.Close;
  end;
  FTable.Open;
  Result := FTable.RecordCount > 0;
end;

{ TAppBOMChildKaynakTakvimKart }

procedure TAppBOMChildKaynakTakvimKart.Append(AVardiyaKod, AKaynakKod: String;
  ABasTarih, ABitTarih: TDateTime);
begin
  if not FTable.Active then
  begin
    Open(AVardiyaKod, AKaynakKod, ABasTarih, ABitTarih);
    FTable.Data := FTableIn.Data;
  end
  else begin
    FTable.SetRange([AVardiyaKod, AKaynakKod, ABasTarih], [AVardiyaKod, AKaynakKod, ABitTarih]);
    FTable.First;
    if FTable.IsEmpty then
    begin
      Open(AVardiyaKod, AKaynakKod, ABasTarih, ABitTarih);
      FTable.AppendTable(FTableIn);
    end;
  end;
end;

procedure TAppBOMChildKaynakTakvimKart.CreateObjects;
begin
  FTableIn := TTableUretimKaynakTakvim.Create(nil);
  FTableIn.Connection := Connection;
  FTableIn.ReadOnly := True;

  FTable := TTableUretimKaynakTakvim.Create(nil);
  FTable.IndexFieldNames := IndexFieldNames;
  FTable.EnableLogChanges := False;
end;

procedure TAppBOMChildKaynakTakvimKart.DisableControls;
begin
  FTable.DisableControls;
end;

procedure TAppBOMChildKaynakTakvimKart.EnableControls;
begin
  FTable.DisableControls;
end;

function TAppBOMChildKaynakTakvimKart.FirstEmpty(AVardiyaKod, AKaynakKod: String;
  ATarih: TDateTime): TDateTime;
begin
  while True do
  begin
    Range(AVardiyaKod, AKaynakKod, ATarih);
    FTable.First;
    while not FTable.Eof do
    begin
      if (FTable.EtkinKapasite - FTable.KullanilanKapasite - FTable.Kullanilan) > 0 then
      begin
        Result := FTable.BasTarihSaat + FTable.KullanilanKapasite + FTable.Kullanilan;
        exit;
      end;
      FTable.Next;
    end;
    ATarih := Trunc(ATarih) + 1;
  end;
end;

procedure TAppBOMChildKaynakTakvimKart.FreeObjects;
begin
  FreeAndNil(FTableIn);
  FreeAndNil(FTable);
end;

function TAppBOMChildKaynakTakvimKart.GetIndexFieldNames: String;
begin
  Result := 'VARDIYAKOD;KAYNAKKOD;TARIH;BASTARIHSAAT';
end;

function TAppBOMChildKaynakTakvimKart.GetKapasite(AVardiyaKod, AKaynakKod: String;
  ABasTarih, ABitTarih: TDateTime): Double;
begin
  Result := 0;
  Range(AVardiyaKod, AKaynakKod, ABasTarih, ABitTarih);
  FTable.First;
  while not FTable.Eof do
  begin
    Result := Result + (FTable.ToplamKapasite - FTable.KullanilanKapasite - FTable.Kullanilan);
    FTable.Next;
  end;
end;

procedure TAppBOMChildKaynakTakvimKart.IncKullanilan(AKullanilanSure: Double);
begin
  FTable.Edit;
  FTable.Kullanilan := FTable.Kullanilan + AKullanilanSure;
  FTable.Post;
end;

function TAppBOMChildKaynakTakvimKart.IsEmpty(AVardiyaKod, AKaynakKod: String;
  ATarih: TDateTime): Boolean;
begin
  Result := True;
  Range(AVardiyaKod, AKaynakKod, ATarih);
  FTable.First;
  while not FTable.Eof do
  begin
    if (FTable.EtkinKapasite - FTable.KullanilanKapasite - FTable.Kullanilan) > 0 then
      exit;
    FTable.Next;
  end;
  Result := False;
end;

procedure TAppBOMChildKaynakTakvimKart.Open(AVardiyaKod, AKaynakKod: String;
  ATarih: TDateTime; const ABitTarih: TDateTime);
begin
  FTableIn.Close;
  FTableIn.TableItems.TableNames := VarArrayOf(['URTKYT']);
  with FTableIn.TableItems[0] do //URTKYT
  begin
    with Fields do
    begin
      Clear;
      Add('*');
      AddExpression('CAST(0 AS NUMERIC(25, 6))', '_KULLANILAN');
    end;
    with Where do
    begin
      Clear;
      Add('SIRKETNO', wcEqual, CompanyNo);
      AddOperator(woAnd);
      Add('KAYITTUR', wcEqual, 1);
      AddOperator(woAnd);
      Add('KAYITDURUM', wcEqual, 1);
      AddOperator(woAnd);
      Add('KAYNAKKOD', wcEqual, AKaynakKod);
      AddOperator(woAnd);
      Add('VARDIYAKOD', wcEqual, AVardiyaKod);
      AddOperator(woAnd);
      if  ABitTarih = 0 then
        Add('TARIH', wcEqual, ATarih)
      else begin
        BeginGroup();
          Add('TARIH', wcGreaterEqual, ATarih);
          AddOperator(woAnd);
          Add('TARIH', wcLessEqual, ABitTarih);
        EndGroup();
      end;
    end;
  end;
  if FTableIn.FieldCount = 0 then
  begin
    FTableIn.DoInitializeRecord;
    FTableIn.Close;
  end;
  FTableIn.Open;
end;

procedure TAppBOMChildKaynakTakvimKart.Range(AVardiyaKod, AKaynakKod: String;
  ATarih: TDateTime);
var
  ABasTarih: TDateTime;
begin
  ABasTarih := Trunc(ATarih);
  if not FTable.Active then
  begin
    Open(AVardiyaKod, AKaynakKod, ABasTarih);
    FTable.Data := FTableIn.Data;
  end
  else begin
    FTable.SetRange([AVardiyaKod, AKaynakKod, ABasTarih], [AVardiyaKod, AKaynakKod, ABasTarih]);
    FTable.First;
    {if FTable.IsEmpty and FCheckEmpty then
    begin
      Open(AVardiyaKod, AKaynakKod, ABasTarih);
      FTable.AppendTable(FTableIn);
    end;
    }
  end;
end;

procedure TAppBOMChildKaynakTakvimKart.Range(AVardiyaKod, AKaynakKod: String; ABasTarih,
  ABitTarih: TDateTime);
begin
  Append(AVardiyaKod, AKaynakKod, ABasTarih, ABitTarih);
  FTable.SetRange([AVardiyaKod, AKaynakKod, ABasTarih], [AVardiyaKod, AKaynakKod, ABitTarih]);
end;

procedure TAppBOMChildKaynakTakvimKart.Reset;
begin
  if not FTable.Active then
    Exit;
  FTable.CancelRange;
  FTable.First;
  while not FTable.Eof do
  begin
    FTable.Edit;
    FTable.Kullanilan := 0;
    FTable.Post;
    FTable.Next;
  end;
end;

{ TAppDataControllerBOMObject }

procedure TAppDataControllerBOMObject.AddChild(AChild: TAppBOMObjectChild);
begin
  if FChildList.IndexOf(AChild) = -1 then
    FChildList.Add(AChild);
end;

constructor TAppDataControllerBOMObject.Create;
begin
  inherited Create;

  FChildList := TList.Create;

  FStokKart := TAppBOMChildStokKart.Create(Self);
  FStokKartVersiyon := TAppBOMChildStokKartVersiyon.Create(Self);
  FStokKartBirim := TAppBOMChildStokKartBirim.Create(Self);
  FStokKartAlternatif := TAppBOMChildStokKartAlternatif.Create(Self);
  FCariKart := TAppBOMChildCariKart.Create(Self);
  FHesapStokKart := TAppBOMChildHesapStokKart.Create(Self);
  FMRPAlanKart := TAppBOMChildMRPAlanKart.Create(Self);
  FMRPAlanStokKart := TAppBOMChildMRPAlanStokKart.Create(Self);
  FDepoKart := TAppBOMChildDepoKart.Create(Self);
  FDepoStokKart := TAppBOMChildDepoStokKart.Create(Self);
  FMRPParametre := TAppBOMChildMRPParametre.Create(Self);

  FIsMerkezKart := TAppBOMChildIsMerkezKart.Create(Self);
  FKaynakKart := TAppBOMChildKaynakKart.Create(Self);
  FIsMerkezKaynakKart := TAppBOMChildIsMerkezKaynakKart.Create(Self);
  FMamulBaslik := TAppBOMChildMamulBaslik.Create(Self);
  FMamulKart := TAppBOMChildMamulKart.Create(Self);
  FOzelMamulKart := TAppBOMChildOzelMamulKart.Create(Self);
  FMamulRotaKart := TAppBOMChildMamulRotaKart.Create(Self);
  FMamulRotaKaynakKart := TAppBOMChildMamulRotaKaynakKart.Create(Self);
  FTakvimVardiyaKart := TAppBOMChildTakvimVardiyaKart.Create(Self);
  FKaynakIslem := TAppBOMChildKaynakIslemTanim.Create(Self);
  FMamulDegisim := TAppBOMChildKaynakMamulDegisim.Create(Self);
  FMamulKurulum := TAppBOMChildKaynakMamulKurulum.Create(Self);
  FKaynakTakvimKart := TAppBOMChildKaynakTakvimKart.Create(Self);
end;

destructor TAppDataControllerBOMObject.Destroy;
var
  I: Integer;
begin
  for I := FChildList.Count - 1 downto 0 do
    TAppBOMObjectChild(FChildList[I]).Free;
  FreeAndNil(FChildList);
  inherited;
end;

procedure TAppDataControllerBOMObject.DisableControls;
var
  I: Integer;
begin
  for I := 0 to FChildList.Count - 1  do
    TAppBOMObjectChild(FChildList[I]).DisableControls;
end;

procedure TAppDataControllerBOMObject.EnableControls;
var
  I: Integer;
begin
  for I := 0 to FChildList.Count - 1  do
    TAppBOMObjectChild(FChildList[I]).EnableControls;
end;

function TAppDataControllerBOMObject.GetCompanyNo: String;
begin
  Result := AppSecurity.DBCompanyNo;
end;

function TAppDataControllerBOMObject.GetConnection: TAppConnection;
begin
  Result := AppSecurity.ConnectionApp;
end;

procedure TAppDataControllerBOMObject.Remove(AChild: TAppBOMObjectChild);
begin
  if FChildList.IndexOf(AChild) <> -1 then
    FChildList.Remove(AChild);
end;

{ TAppBOMChildTakvimVardiyaKart }

procedure TAppBOMChildTakvimVardiyaKart.CreateObjects;
begin
  FTable := TTableUretimTakvimVardiya.Create(nil);
  FTable.Connection := Connection;
  FTable.ReadOnly := True;
  FTable.IndexFieldNames := IndexFieldNames;
end;

procedure TAppBOMChildTakvimVardiyaKart.DisableControls;
begin
  FTable.DisableControls;
end;

procedure TAppBOMChildTakvimVardiyaKart.EnableControls;
begin
  FTable.EnableControls;
end;

procedure TAppBOMChildTakvimVardiyaKart.FreeObjects;
begin
  FreeAndNil(FTable);
end;

function TAppBOMChildTakvimVardiyaKart.GetIndexFieldNames: String;
begin
  Result := 'TAKVIMKOD'; // VARDIYA INDEX DE VARDI kaldırıldı, id ye göre alması lazım
end;

procedure TAppBOMChildTakvimVardiyaKart.Open;
begin
  FTable.Close;
  FTable.TableItems.TableNames := 'URTTKV';
  with FTable.TableItems[0] do
  begin
    with Fields do
    begin
      Clear;
      Add('TAKVIMKOD');
      Add('VARDIYAKOD');
    end;
  end;
  FTable.Open;
end;

procedure TAppBOMChildTakvimVardiyaKart.Range(ATakvimKod: String);
begin
  if not FTable.Active then
    Open;
  FTable.SetRange([ATakvimKod], [ATakvimKod]);
end;

{ TAppBOMChildKaynakMamulKurulum }

procedure TAppBOMChildKaynakMamulKurulum.CreateObjects;
begin
  FTable := TTableUretimKaynakMamulKurulum.Create(nil);
  FTable.Connection := Connection;
  FTable.ReadOnly := True;
  FTable.IndexFieldNames := IndexFieldNames;
end;

procedure TAppBOMChildKaynakMamulKurulum.DisableControls;
begin
  FTable.DisableControls;
end;

procedure TAppBOMChildKaynakMamulKurulum.EnableControls;
begin
  FTable.EnableControls;
end;

function TAppBOMChildKaynakMamulKurulum.Find(AKaynakKod, AMamulKod: String): Boolean;
begin
  Result := Open(AKaynakKod, AMamulKod);
end;

procedure TAppBOMChildKaynakMamulKurulum.FreeObjects;
begin
  FreeAndNil(FTable);
end;

function TAppBOMChildKaynakMamulKurulum.GetIndexFieldNames: String;
begin
  Result := 'KAYNAKKOD;MAMULKOD';
end;

function TAppBOMChildKaynakMamulKurulum.Open(AKaynakKod, AMamulKod: String): Boolean;
begin
  FTable.Close;
  with FTable.TableItems[0] do // URTKMK
  begin
    with Where do
    begin
      Clear;
      Add('KAYNAKKOD', wcEqual, AKaynakKod);
      AddOperator(woAnd);
      Add('MAMULKOD', wcEqual, AMamulKod);
    end;
  end;
  if FTable.FieldCount = 0 then
  begin
    FTable.DoInitializeRecord;
    FTable.Close;
  end;
  FTable.Open;
  Result := FTable.RecordCount > 0;
end;

{ TAppBOMChildStokKartVersiyon }

procedure TAppBOMChildStokKartVersiyon.Close;
begin
  FTable.Close;
end;

procedure TAppBOMChildStokKartVersiyon.CreateObjects;
begin
  FTable := TTableStokVersiyon.Create(nil);
  FTable.Connection := Connection;
  FTable.ReadOnly := True;
  FTable.IndexFieldNames := IndexFieldNames; //'MALKOD;VERSIYONNO';
  FTable.TableItems.TableNames := 'STKVER';
end;

procedure TAppBOMChildStokKartVersiyon.DisableControls;
begin
  FTable.DisableControls;
end;

procedure TAppBOMChildStokKartVersiyon.EnableControls;
begin
  FTable.EnableControls;
end;

function TAppBOMChildStokKartVersiyon.Find(AMalKod, AVersiyonNo: String): Boolean;
begin
  Result := FTable.FindKey([AMalKod, AVersiyonNo]);
end;

procedure TAppBOMChildStokKartVersiyon.FreeObjects;
begin
  FreeAndNil(FTable);
end;

function TAppBOMChildStokKartVersiyon.GetIndexFieldNames: String;
begin
  Result := 'MALKOD;VERSIYONNO';
end;

procedure TAppBOMChildStokKartVersiyon.Open;
begin
  FTable.Close;
  if FTable.FieldCount = 0 then
  begin
    FTable.DoInitializeRecord;
    FTable.Close;
  end;
  FTable.Open;
end;

{ TAppBOMChildOzelMamulKart }

procedure TAppBOMChildOzelMamulKart.Close;
begin
  FTable.Close;
end;

procedure TAppBOMChildOzelMamulKart.CreateObjects;
begin
  FTable := TTableOzelMamulKart.Create(nil);
  FTable.Connection := Connection;
  FTable.ReadOnly := True;
  SetDefinitions;
end;

procedure TAppBOMChildOzelMamulKart.DisableControls;
begin
  FTable.DisableControls;
end;

procedure TAppBOMChildOzelMamulKart.EnableControls;
begin
  FTable.EnableControls;
end;

procedure TAppBOMChildOzelMamulKart.FreeObjects;
begin
  FreeAndNil(FTable);
end;

function TAppBOMChildOzelMamulKart.GetIndexFieldNames: String;
begin
  Result := 'SIRANO';
end;

procedure TAppBOMChildOzelMamulKart.Open(AEvrakTip: Smallint; AHesapKod,
  AEvrakNo: String; ASiraNo: Integer; AMamulKod, AMamulVersiyonNo: String; AMamulSurumNo: Integer);
begin
  FTable.Close;
  with FTable.TableItems[0] do
  begin
    with Where do
    begin
      Clear;
      Add('SIRKETNO', wcEqual, CompanyNo);
      AddOperator(woAnd);
      Add('EVRAKTIP', wcEqual, AEvrakTip);
      AddOperator(woAnd);
      Add('HESAPKOD', wcEqual, AHesapKod);
      AddOperator(woAnd);
      Add('EVRAKNO', wcEqual, AEvrakNo);
      AddOperator(woAnd);
      Add('EVRAKSIRANO', wcEqual, ASiraNo);
      AddOperator(woAnd);
      Add('MAMULKOD', wcEqual, AMamulKod);
      AddOperator(woAnd);
      Add('MAMULVERSIYONNO', wcEqual, AMamulVersiyonNo);
      AddOperator(woAnd);
      Add('MAMULSURUMNO', wcEqual, AMamulSurumNo);
    end;
  end;
  if FTable.FieldCount = 0 then
  begin
    FTable.DoInitializeRecord;
    FTable.Close;
  end;
  FTable.Open;
end;

procedure TAppBOMChildOzelMamulKart.SetDefinitions;
begin
  FTable.IndexFieldNames := IndexFieldNames;
  // Set Table Definitions
  FTable.TableItems.TableNames := VarArrayOf(['STHOMK', 'STKKRT', 'STKBRM', 'STKKRT', 'STKBRM']);
  FTable.TableItems.TableAlias := VarArrayOf(['STHOMK', 'MAMSTK', 'MAMBRM', 'STKKRT', 'STKBRM']);
  FTable.TableItems.TableReferans := VarArrayOf(['STHOMK', 'STKKRT', 'STKBRM', 'STKKRT', 'STKBRM']);
  FTable.TableItems.TableCaptions := VarArrayOf(['Özel Mamül Kart', 'Mamül', 'Mamül Birim', 'Hammadde', 'Hammadde Birim']);

  with FTable.TableItems[0] do // STHOMK --> Özel Mamül Kartı Tablosu
  begin
    with Fields do
    begin
      Clear;
      Add('*');
    end;
  end;
  with FTable.TableItems[1] do // MAMSTK --> Mamül Stok Kartı Tablosu (In Real STKKRT)
  begin
    with Fields do
    begin
      Clear;
      Add('BIRIM');
      Add('YUVARLAMA');
      Add('MONTAJFIREORAN');
    end;
    with Join do
    begin
      Clear;
      Add('MALKOD', 'MAMULKOD');
    end;
  end;
  with FTable.TableItems[2] do // MAMBRM --> Mamül Stok Birimleri Tablosu (In Real STKBRM)
  begin
    with Fields do
    begin
      Clear;
      AddExpression('ISNULL(MAMBRM.KATSAYI, -1)', 'MAMBRM_KATSAYI');
    end;
    with Join do
    begin
      Clear;
      Add('MALKOD', 'MAMULKOD');
      Add('BIRIM', 'BIRIM', FTable.TableItems[1]);
    end;
  end;
  with FTable.TableItems[3] do // STKKRT --> Hammadde Stok Kartı Tablosu
  begin
    with Fields do
    begin
      Clear;
      Add('BIRIM');
      Add('MRPTIP');
      Add('TEMINTIP');
      Add('TEMINYONTEM');
      Add('BILESENFIREORAN');
      Add('YUVARLAMA');
      Add('GRUPNO');
    end;
    with Join do
    begin
      Clear;
      Add('MALKOD', 'HAMMADDEKOD');
    end;
  end;
  with FTable.TableItems[4] do // STKBRM --> Hammadde Stok Birim Tablosu
  begin
    with Fields do
    begin
      Clear;
      AddExpression('ISNULL(STKBRM.KATSAYI, -1)', 'STKBRM_KATSAYI');
    end;
    with Join do
    begin
      Clear;
      Add('MALKOD', 'HAMMADDEKOD');
      Add('BIRIM', 'HAMMADDEBIRIM');
    end;
  end;
end;

{ TAppBOMChildStokKartAlternatif }

procedure TAppBOMChildStokKartAlternatif.Close;
begin
  FTable.Close;
end;

procedure TAppBOMChildStokKartAlternatif.CreateObjects;
begin
  FTable := TTableStokKartAlternatif.Create(nil);
  FTable.Connection := Connection;
  FTable.ReadOnly := True;
  FTable.IndexFieldNames := IndexFieldNames;
  FTable.TableItems.TableNames := 'STKALT';
end;

procedure TAppBOMChildStokKartAlternatif.DisableControls;
begin
  FTable.DisableControls;
end;

procedure TAppBOMChildStokKartAlternatif.EnableControls;
begin
  FTable.EnableControls;
end;

function TAppBOMChildStokKartAlternatif.Find(AMalKod, AVersiyonNo: String): Boolean;
begin
  Result := FTable.FindKey([AMalKod, AVersiyonNo]);
end;

procedure TAppBOMChildStokKartAlternatif.FreeObjects;
begin
  FreeAndNil(FTable);
end;

function TAppBOMChildStokKartAlternatif.GetIndexFieldNames: String;
begin
  Result := 'MALKOD;VERSIYONNO;SIRANO';
end;

procedure TAppBOMChildStokKartAlternatif.Open;
begin
  FTable.Close;
  if FTable.FieldCount = 0 then
  begin
    FTable.DoInitializeRecord;
    FTable.Close;
  end;
  FTable.Open;
end;

{ TAppBOMChildKaynakIslemTanim }

procedure TAppBOMChildKaynakIslemTanim.CreateObjects;
begin
  FTable := TTableUretimKaynakIslemTanim.Create(nil);
  FTable.Connection := Connection;
  FTable.ReadOnly := True;
  FTable.IndexFieldNames := IndexFieldNames;
end;

procedure TAppBOMChildKaynakIslemTanim.DisableControls;
begin
  FTable.DisableControls;
end;

procedure TAppBOMChildKaynakIslemTanim.EnableControls;
begin
  FTable.EnableControls;
end;

procedure TAppBOMChildKaynakIslemTanim.FreeObjects;
begin
  FreeAndNil(FTable);
end;

function TAppBOMChildKaynakIslemTanim.GetIndexFieldNames: String;
begin
  Result := 'ISLEMNO';
end;

function TAppBOMChildKaynakIslemTanim.Open(AKaynakKod: String): Boolean;
begin
  FTable.Close;
  with FTable.TableItems[0] do // URTKIT
  begin
    with Where do
    begin
      Clear;
      Add('KAYNAKKOD', wcEqual, AKaynakKod);
    end;
  end;
  if FTable.FieldCount = 0 then
  begin
    FTable.DoInitializeRecord;
    FTable.Close;
  end;
  FTable.Open;
  Result := FTable.RecordCount > 0;
end;

end.

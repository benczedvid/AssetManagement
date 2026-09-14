export const COUNTRY_CODES = [
    'Unknown',
    'HUN',
    'GER',
    'HRV',
    'AUT',
    'SVK',
    'UKR',
    'ROU',
    'SRB',
    'POL'
] as const;

export type CountryCode = typeof COUNTRY_CODES[number];

export const PUBLIC_SPACES = [
    'Unknown',
    'MainRoad',
    'BoundaryRoad',
    'Alley',
    'Meadow',
    'Row',
    'Boulevard',
    'Lane',
    'Square',
    'Street',
    'Road'
] as const;

export type PublicSpace = typeof PUBLIC_SPACES[number];

export interface Address {
    countryCode: CountryCode
    postalCode: string
    city: string
    street: string
    publicSpace: PublicSpace
    houseNumber: string
}

export const COUNTRY_CODE_LABELS: Record<CountryCode, string> = {
    Unknown: 'Ismeretlen',
    HUN: 'Magyarország',
    GER: 'Németország',
    HRV: 'Horvátország',
    AUT: 'Ausztria',
    SVK: 'Szlovákia',
    UKR: 'Ukrajna',
    ROU: 'Románia',
    SRB: 'Szerbia',
    POL: 'Lengyelország'
};

export const PUBLIC_SPACE_LABELS: Record<PublicSpace, string> = {
  Unknown: 'Ismeretlen',
  MainRoad: 'főút',
  BoundaryRoad: 'határút',
  Alley: 'köz',
  Meadow: 'rét',
  Row: 'sor',
  Boulevard: 'körút',
  Lane: 'köz',
  Square: 'tér',
  Street: 'utca',
  Road: 'út'
};
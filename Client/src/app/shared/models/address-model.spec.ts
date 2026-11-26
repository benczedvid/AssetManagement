import { Address, formatAddress } from './address-model';

describe('formatAddress', () => {
  const address: Address = { countryCode: 'HUN', postalCode: '1117', city: 'Budapest', street: 'Budafoki', publicSpace: 'Road', houseNumber: '56' };

  it('formats a Hungarian address with the Hungarian public space label', () => {
    expect(formatAddress(address)).toBe('1117 Budapest, Budafoki út 56.');
  });

  it('prepends the country for foreign addresses and keeps an existing trailing dot', () => {
    expect(formatAddress({ ...address, countryCode: 'AUT', houseNumber: '12/B.' })).toBe('Ausztria, 1117 Budapest, Budafoki út 12/B.');
  });

  it('omits an unknown public space', () => {
    expect(formatAddress({ ...address, publicSpace: 'Unknown' })).toBe('1117 Budapest, Budafoki 56.');
  });
});

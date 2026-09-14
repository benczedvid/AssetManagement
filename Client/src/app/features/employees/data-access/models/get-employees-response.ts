export interface GetEmployeesResponse {
  readonly employeeNumber: string;
  readonly fullName: string;
  readonly employmentStartDate: string;
  readonly employmentEndDate: string | null;
  readonly storeNumber: string;
  readonly organizationType: string;
  readonly positionCode: string;
  readonly position: string;
  readonly phoneNumber: string | null;
  readonly email: string | null;
}

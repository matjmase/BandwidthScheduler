import { UserLegendModel } from '../../../commonControls/user-legend/user-legend-model';
import { ColoredTimeFrameModel } from '../../schedule-publisher/ColoredTimeFrameModel';

export interface IGridLegendReadOnlyModel {
  LegendModel: UserLegendModel[];
  ColoredFrames: ColoredTimeFrameModel[];
}

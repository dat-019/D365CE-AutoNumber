## Overview

A custom auto number solution is required to set various auto number fields.

## Process Overview

The Auto number generation is required for many entities which should cater for a wider range of numbers with different formats. Below are some examples of formatted pattern for auto number value.

| Entity        | Entity Field Label    | Format                  | Prefix  | Increment      | Example     |
| ------------- |:----------------------|:------------------------|:--------|:---------------|:------------|
| Account       | Account_Id            |Prefix + 8 digits number |ACC-     |last number + 1 |ACC-00000001 |
| Contact       | Contact_Id            |Prefix + 8 digits number |PER-     |last number + 1 |PER-00000001 |

## Design Notes

The Auto Number solution framework needs to be able to generate padded strings in various formats. A configuration entity “Auto Numbers” will be added to manage the auto numbers going forward. The Plugin will read the Auto Number format for each entity from this configuration entity.

### Configuration entity "Auto Numbers"

 ![A screenshot of the Auto number configuration entity ](/images/AutoNumberConfiguration.png)
 
### Plugin - Support Optimistic Concurrency

#### Source code - ref at: [AutonumberGeneration](https://github.com/dat-019/D365CE-AutoNumber/blob/master/AutoNumberGeneration/AutonumberGeneration.cs)

#### Plugin Registration

 ![A screenshot of the plugin registration ](/images/PluginRegistrationStep.png)
 
#### Example - Auto number configured for Account entity

 ![A screenshot of auto number configured for Account ](/images/autonumberconfiguredforaccount.png)

## Reference
- Optimistic concurrency: https://docs.microsoft.com/en-us/powerapps/developer/common-data-service/optimistic-concurrency
